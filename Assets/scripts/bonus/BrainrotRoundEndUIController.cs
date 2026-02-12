using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class BrainrotRoundEndUIController : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text earnedCoinsText;
    [SerializeField] private Text rewardTitleText;
    [SerializeField] private Text rewardDetailText;
    [SerializeField] private CanvasGroup rewardCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float introDelay = 0.2f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private float revealDuration = 0.4f;

    public const string EarnedCoinsFormat = "You earned {0} coins!";
    public const string ChestRewardTitle = "Chest Reward!";
    public static readonly string ChestRewardDetail = $"+{BrainrotStarTracker.ChestConsumeCoinReward} coins +{BrainrotStarTracker.ChestConsumeExtraLifeReward}UP";
    public const string PerfectBonusDetail = "Bonus: +1 coin";

    private Coroutine revealRoutine;

    public void ShowRoundEnd(int correctAnswers, bool grandQuestionCorrect)
    {
        var allCorrect = correctAnswers == BrainrotTriviaBonusGame.QuestionsPerRound;
        var roundCoins = allCorrect
            ? BrainrotTriviaBonusGame.QuestionsPerRound + BrainrotTriviaBonusGame.PerfectRoundBonusCoins
            : correctAnswers;

        root.SetActive(true);
        earnedCoinsText.text = string.Format(EarnedCoinsFormat, roundCoins);
        rewardTitleText.text = string.Empty;
        rewardDetailText.text = string.Empty;

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(PlayRewardReveal(allCorrect, grandQuestionCorrect));
    }

    private IEnumerator PlayRewardReveal(bool allCorrect, bool grandQuestionCorrect)
    {
        rewardCanvasGroup.alpha = 0f;
        rewardCanvasGroup.transform.localScale = Vector3.one * 0.9f;

        yield return new WaitForSeconds(introDelay);

        if (!allCorrect)
            yield break;

        if (grandQuestionCorrect)
        {
            rewardTitleText.text = ChestRewardTitle;
            rewardDetailText.text = ChestRewardDetail;
        }
        else
        {
            rewardTitleText.text = string.Empty;
            rewardDetailText.text = PerfectBonusDetail;
        }

        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            rewardCanvasGroup.alpha = t;
            rewardCanvasGroup.transform.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);
            yield return null;
        }

        float pulseElapsed = 0f;
        while (pulseElapsed < pulseDuration)
        {
            pulseElapsed += Time.deltaTime;
            float pulseT = Mathf.Sin((pulseElapsed / pulseDuration) * Mathf.PI);
            rewardCanvasGroup.transform.localScale = Vector3.one * (1f + (pulseT * 0.06f));
            yield return null;
        }

        rewardCanvasGroup.transform.localScale = Vector3.one;
    }
}
