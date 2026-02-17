const functions = require("firebase-functions");
const admin = require("firebase-admin");

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

function validatePayload(data) {
  if (!data || typeof data !== "object") {
    return {valid: false, rejection: rejectedResponse("invalid-payload", "Payload must be an object.")};
  }

  const {mode, runId, runStart, runEnd} = data;

  if (!VALID_MODES.includes(mode)) {
    return {
      valid: false,
      rejection: rejectedResponse("invalid-mode", "mode must be one of: normal, hardcore, ironman."),
    };
  }

  if (typeof runId !== "string" || runId.trim().length === 0) {
    return {valid: false, rejection: rejectedResponse("invalid-run-id", "runId is required.")};
  }

  if (!Number.isInteger(runStart) || !Number.isInteger(runEnd)) {
    return {
      valid: false,
      rejection: rejectedResponse("invalid-timestamps", "runStart and runEnd must be Unix timestamps in seconds."),
    };
  }

  if (runEnd <= runStart) {
    return {
      valid: false,
      rejection: rejectedResponse("invalid-duration-order", "runEnd must be after runStart."),
    };
  }

  const runDuration = runEnd - runStart;
  if (runDuration < MIN_RUN_DURATION_SECONDS) {
    return {
      valid: false,
      rejection: rejectedResponse(
          "run-too-short",
          `Run duration must be at least ${MIN_RUN_DURATION_SECONDS} seconds.`,
      ),
    };
  }

  return {
    valid: true,
    payload: {
      mode,
      runId: runId.trim(),
      runStart,
      runEnd,
      runDuration,
    },
  };
}

exports.incrementWeeklyModeWin = functions.https.onCall(async (data, context) => {
  if (!context.auth || !context.auth.uid) {
    throw new functions.https.HttpsError(
        "unauthenticated",
        "Authentication is required to submit a weekly win.",
    );
  }

  const validation = validatePayload(data);
  if (!validation.valid) {
    return validation.rejection;
  }

  const {mode, runId, runStart, runEnd, runDuration} = validation.payload;
  const uid = context.auth.uid;
  const weekKey = getCurrentWeekKey();

  const weeklyDocRef = db.collection("weekly_stats").doc(weekKey);
  const runDocId = `${uid}_${runId}`;
  const runDocRef = weeklyDocRef.collection("submitted_runs").doc(runDocId);

  try {
    await db.runTransaction(async (transaction) => {
      const existingRunDoc = await transaction.get(runDocRef);
      if (existingRunDoc.exists) {
        throw new functions.https.HttpsError(
            "already-exists",
            "This runId has already been submitted by this user.",
        );
      }

      transaction.set(runDocRef, {
        uid,
        mode,
        runId,
        runStart,
        runEnd,
        runDuration,
        createdAt: admin.firestore.FieldValue.serverTimestamp(),
      });

      transaction.set(
          weeklyDocRef,
          {
            [mode]: admin.firestore.FieldValue.increment(1),
            updatedAt: admin.firestore.FieldValue.serverTimestamp(),
          },
          {merge: true},
      );
    });
  } catch (error) {
    if (error instanceof functions.https.HttpsError && error.code === "already-exists") {
      return rejectedResponse("duplicate-run-id", error.message);
    }

    functions.logger.error("incrementWeeklyModeWin failed unexpectedly", error);
    throw new functions.https.HttpsError("internal", "Unexpected error while processing weekly win submission.");
  }

  return {
    success: true,
    status: "accepted",
    message: "Weekly win counted successfully.",
    weekKey,
    mode,
    runId,
    runDuration,
  };
});
