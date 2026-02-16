using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float loadingDuration = 0.75f;

    [Header("Death Flow")]
    [SerializeField] private bool allowDeathAutoRetry = true;
    [SerializeField] private GameOverConfig gameOverConfig;
    [SerializeField] private TipsConfig tipsConfig;
    [SerializeField] private int startingOneUps;
    [SerializeField] private bool hardcoreModeEnabled;

    public event Action<GameState> StateChanged;
    public event Action AutoRetryPopupRequested;
    public event Action ResurrectionStarted;
    public event Action<string> ResurrectionVideoRequested;
    public event Action<string> ResurrectionTipRequested;
    public event Action<bool> ShopOfferOneUpChanged;

    /// <summary>
    /// Hook this from your level/gameplay systems.
    /// Return true when auto-retry should happen (e.g., player has 3 stars).
    /// The hook is also responsible for consuming/resetting those stars.
    /// </summary>
    public Func<bool> TryAutoRetryOnDeath;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int OneUps { get; private set; }

    private Coroutine loadingRoutine;
    private Coroutine resurrectionRoutine;
    private const string TipsCycleIndexPrefsKey = "SixSeven.Tips.NextIndex";
    private int nextTipIndex;

    private void Start()
    {
        OneUps = Mathf.Max(0, startingOneUps);
        nextTipIndex = Mathf.Max(0, PlayerPrefs.GetInt(TipsCycleIndexPrefsKey, 0));
        SetState(GameState.MainMenu);
    }

    public void StartGame()
    {
        if (CurrentState == GameState.Loading || CurrentState == GameState.Playing)
            return;

        SetState(GameState.Loading);
        BeginLoading();
    }

    public void RetryLevel()
    {
        if (CurrentState != GameState.GameOver)
            return;

        StartGame();
    }

    public void CompleteLevel()
    {
        if (CurrentState != GameState.Playing)
            return;

        SetState(GameState.LevelComplete);
    }

    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing)
            return;

        if (ShouldUseOneUp())
        {
            OneUps = Mathf.Max(0, OneUps - 1);
            BeginResurrectionFlow();
            return;
        }

        if (allowDeathAutoRetry && TryAutoRetryOnDeath?.Invoke() == true)
        {
            AutoRetryPopupRequested?.Invoke();
            StartLoadingFromCurrentLevel();
            return;
        }

        SetState(GameState.GameOver);
    }

    public void EnterShop()
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.LevelComplete)
            return;

        SetState(GameState.Shop);
        ShopOfferOneUpChanged?.Invoke(ForceOfferOneUpInShop());
    }

    public void ReturnToMainMenu()
    {
        if (CurrentState == GameState.MainMenu)
            return;

        StopLoadingRoutine();
        SetState(GameState.MainMenu);
    }

    private void BeginLoading()
    {
        StopLoadingRoutine();
        loadingRoutine = StartCoroutine(LoadingRoutine());
    }

    private void StartLoadingFromCurrentLevel()
    {
        SetState(GameState.Loading);
        BeginLoading();
    }

    private IEnumerator LoadingRoutine()
    {
        if (loadingDuration > 0f)
            yield return new WaitForSeconds(loadingDuration);

        SetState(GameState.Playing);
        loadingRoutine = null;
    }

    private void StopLoadingRoutine()
    {
        if (loadingRoutine == null)
            return;

        StopCoroutine(loadingRoutine);
        loadingRoutine = null;
    }

    private bool ShouldUseOneUp()
    {
        if (OneUps <= 0)
            return false;

        if (gameOverConfig != null && !gameOverConfig.consumeOneUpOnDeath)
            return false;

        if (hardcoreModeEnabled && gameOverConfig != null && gameOverConfig.allowHardcoreMode)
            return false;

        return true;
    }

    private bool ForceOfferOneUpInShop()
    {
        if (gameOverConfig == null)
            return true;

        return gameOverConfig.shopForceOfferOneUp;
    }

    private void BeginResurrectionFlow()
    {
        if (resurrectionRoutine != null)
            StopCoroutine(resurrectionRoutine);

        resurrectionRoutine = StartCoroutine(ResurrectionRoutine());
    }

    private IEnumerator ResurrectionRoutine()
    {
        ResurrectionStarted?.Invoke();

        var fadeDuration = gameOverConfig != null ? Mathf.Max(0f, gameOverConfig.fadeToBlackDuration) : 0.4f;
        if (fadeDuration > 0f)
            yield return new WaitForSeconds(fadeDuration);

        if (gameOverConfig != null)
        {
            ResurrectionVideoRequested?.Invoke(gameOverConfig.resurrectionVideoPath);
            var videoDuration = Mathf.Max(0f, gameOverConfig.resurrectionVideoDuration);
            if (videoDuration > 0f)
                yield return new WaitForSeconds(videoDuration);

            var nextTip = GetNextTip();
            if (!string.IsNullOrWhiteSpace(nextTip))
                ResurrectionTipRequested?.Invoke(nextTip);
        }

        SetState(GameState.Shop);
        ShopOfferOneUpChanged?.Invoke(true);
        resurrectionRoutine = null;
    }

    private string GetNextTip()
    {
        if (tipsConfig == null || tipsConfig.tips == null || tipsConfig.tips.Count == 0)
            return string.Empty;

        if (nextTipIndex >= tipsConfig.tips.Count)
            nextTipIndex = 0;

        var tipEntry = tipsConfig.tips[nextTipIndex];
        var selectedText = tipEntry != null ? tipEntry.tip : string.Empty;

        nextTipIndex++;
        if (nextTipIndex >= tipsConfig.tips.Count)
            nextTipIndex = 0;

        PlayerPrefs.SetInt(TipsCycleIndexPrefsKey, nextTipIndex);
        PlayerPrefs.Save();

        return selectedText;
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}
