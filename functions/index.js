const functions = require("firebase-functions");
const admin = require("firebase-admin");
const {randomUUID} = require("crypto");

admin.initializeApp();
const db = admin.firestore();

const VALID_MODES = ["normal", "hardcore", "ironman"];
const MIN_RUN_DURATION_SECONDS = 300;

function getCurrentWeekKey(date = new Date()) {
  const utcDate = new Date(Date.UTC(
      date.getUTCFullYear(),
      date.getUTCMonth(),
      date.getUTCDate(),
  ));
  const dayNum = utcDate.getUTCDay() || 7;
  utcDate.setUTCDate(utcDate.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(utcDate.getUTCFullYear(), 0, 1));
  const weekNo = Math.ceil((((utcDate - yearStart) / 86400000) + 1) / 7);
  return `${utcDate.getUTCFullYear()}_W${weekNo}`;
}

function rejectedResponse(code, message) {
  return {
    success: false,
    status: "rejected",
    code,
    message,
  };
}

function validateMode(mode) {
  if (!VALID_MODES.includes(mode)) {
    return {
      valid: false,
      rejection: rejectedResponse("invalid-mode", "mode must be one of: normal, hardcore, ironman."),
    };
  }

  return {valid: true, mode};
}

exports.startWeeklyRun = functions.https.onCall(async (data, context) => {
  if (!context.auth || !context.auth.uid) {
    throw new functions.https.HttpsError(
        "unauthenticated",
        "Authentication is required to start a weekly run.",
    );
  }

  if (!data || typeof data !== "object") {
    return rejectedResponse("invalid-payload", "Payload must be an object.");
  }

  const modeValidation = validateMode(data.mode);
  if (!modeValidation.valid) {
    return modeValidation.rejection;
  }

  const uid = context.auth.uid;
  const weekKey = getCurrentWeekKey();
  const runId = randomUUID();
  const activeRunRef = db.collection("active_runs").doc(uid);

  await activeRunRef.set({
    runId,
    mode: modeValidation.mode,
    weekKey,
    serverStartTimestamp: admin.firestore.FieldValue.serverTimestamp(),
  });

  return {
    success: true,
    status: "accepted",
    runId,
    weekKey,
  };
});

exports.completeWeeklyRun = functions.https.onCall(async (data, context) => {
  if (!context.auth || !context.auth.uid) {
    throw new functions.https.HttpsError(
        "unauthenticated",
        "Authentication is required to complete a weekly run.",
    );
  }

  if (!data || typeof data !== "object") {
    return rejectedResponse("invalid-payload", "Payload must be an object.");
  }

  const {runId, mode} = data;
  if (typeof runId !== "string" || runId.trim().length === 0) {
    return rejectedResponse("invalid-run-id", "runId is required.");
  }

  const modeValidation = validateMode(mode);
  if (!modeValidation.valid) {
    return modeValidation.rejection;
  }

  const uid = context.auth.uid;
  const activeRunRef = db.collection("active_runs").doc(uid);

  try {
    const outcome = await db.runTransaction(async (transaction) => {
      const activeRunSnap = await transaction.get(activeRunRef);
      if (!activeRunSnap.exists) {
        return {rejection: rejectedResponse("active-run-missing", "No active run exists for this user.")};
      }

      const activeRun = activeRunSnap.data();
      if (!activeRun || activeRun.runId !== runId.trim()) {
        return {rejection: rejectedResponse("run-id-mismatch", "runId does not match the active run.")};
      }

      if (activeRun.mode !== modeValidation.mode) {
        return {rejection: rejectedResponse("mode-mismatch", "mode does not match the active run.")};
      }

      if (!activeRun.serverStartTimestamp || typeof activeRun.serverStartTimestamp.toMillis !== "function") {
        return {rejection: rejectedResponse("start-time-unavailable", "Active run start time is not available yet.")};
      }

      const nowMillis = admin.firestore.Timestamp.now().toMillis();
      const startMillis = activeRun.serverStartTimestamp.toMillis();
      const duration = Math.floor((nowMillis - startMillis) / 1000);

      if (duration < MIN_RUN_DURATION_SECONDS) {
        return {
          rejection: rejectedResponse(
              "run-too-short",
              `Run duration must be at least ${MIN_RUN_DURATION_SECONDS} seconds.`,
          ),
        };
      }

      const weekKey = activeRun.weekKey || getCurrentWeekKey();
      const weeklyDocRef = db.collection("weekly_stats").doc(weekKey);
      const completedRunRef = db.collection("completed_runs").doc(`${weekKey}_${uid}_${runId.trim()}`);

      transaction.set(
          weeklyDocRef,
          {
            [modeValidation.mode]: admin.firestore.FieldValue.increment(1),
            updatedAt: admin.firestore.FieldValue.serverTimestamp(),
          },
          {merge: true},
      );

      transaction.set(completedRunRef, {
        uid,
        mode: modeValidation.mode,
        duration,
        completedAt: admin.firestore.FieldValue.serverTimestamp(),
      });

      transaction.delete(activeRunRef);

      return {
        success: true,
        weekKey,
        duration,
      };
    });

    if (outcome.rejection) {
      return outcome.rejection;
    }

    return {
      success: true,
      status: "accepted",
      weekKey: outcome.weekKey,
      duration: outcome.duration,
    };
  } catch (error) {
    functions.logger.error("completeWeeklyRun failed unexpectedly", error);
    throw new functions.https.HttpsError("internal", "Unexpected error while completing weekly run.");
  }
});
