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

function ensureValidPayload(data) {
  if (!data || typeof data !== "object") {
    throw new functions.https.HttpsError("invalid-argument", "Payload must be an object.");
  }

  const {mode, runId, runStart, runEnd} = data;

  if (!VALID_MODES.includes(mode)) {
    throw new functions.https.HttpsError(
        "invalid-argument",
        "mode must be one of: normal, hardcore, ironman.",
    );
  }

  if (typeof runId !== "string" || runId.trim().length === 0) {
    throw new functions.https.HttpsError("invalid-argument", "runId is required.");
  }

  if (!Number.isInteger(runStart) || !Number.isInteger(runEnd)) {
    throw new functions.https.HttpsError(
        "invalid-argument",
        "runStart and runEnd must be Unix timestamps in seconds.",
    );
  }

  if (runEnd <= runStart) {
    throw new functions.https.HttpsError("invalid-argument", "runEnd must be after runStart.");
  }

  const runDuration = runEnd - runStart;
  if (runDuration < MIN_RUN_DURATION_SECONDS) {
    throw new functions.https.HttpsError(
        "failed-precondition",
        `Run duration must be at least ${MIN_RUN_DURATION_SECONDS} seconds.`,
    );
  }

  return {mode, runId: runId.trim(), runStart, runEnd, runDuration};
}

exports.incrementWeeklyModeWin = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError(
        "unauthenticated",
        "Authentication is required to submit a weekly win.",
    );
  }

  const {mode, runId, runStart, runEnd, runDuration} = ensureValidPayload(data);
  const uid = context.auth.uid;
  const weekKey = getCurrentWeekKey();

  const weeklyDocRef = db.collection("weekly_stats").doc(weekKey);
  const runDocRef = weeklyDocRef.collection("submitted_runs").doc(runId);

  await db.runTransaction(async (transaction) => {
    const existingRunDoc = await transaction.get(runDocRef);
    if (existingRunDoc.exists) {
      throw new functions.https.HttpsError(
          "already-exists",
          "This runId has already been submitted.",
      );
    }

    transaction.set(runDocRef, {
      uid,
      mode,
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

  return {
    success: true,
    weekKey,
    mode,
    runId,
  };
});
