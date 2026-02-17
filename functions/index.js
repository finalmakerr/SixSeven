const functions = require("firebase-functions");
const admin = require("firebase-admin");

admin.initializeApp();
const db = admin.firestore();

function getCurrentWeekKey() {
  const now = new Date();
  const oneJan = new Date(now.getFullYear(), 0, 1);
  const days = Math.floor((now - oneJan) / (24 * 60 * 60 * 1000));
  const week = Math.ceil((days + oneJan.getDay() + 1) / 7);
  return `${now.getFullYear()}_W${week}`;
}

exports.incrementWeeklyModeWin = functions.https.onCall(async (data, context) => {
  if (!data || !data.mode) {
    throw new functions.https.HttpsError("invalid-argument", "Mode required.");
  }

  const mode = data.mode;

  if (!["normal", "hardcore", "ironman"].includes(mode)) {
    throw new functions.https.HttpsError("invalid-argument", "Invalid mode.");
  }

  const weekKey = getCurrentWeekKey();
  const docRef = db.collection("weekly_stats").doc(weekKey);

  await docRef.set(
    {
      [mode]: admin.firestore.FieldValue.increment(1),
    },
    { merge: true }
  );

  return { success: true };
});
