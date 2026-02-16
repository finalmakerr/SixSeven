using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float loadingDuration = 0.75f;

    [Header("Death Flow")]
    [SerializeField] private bool allowDeathAutoRetry = true;
    [SerializeField] private GameOverConfig gameOverConfig;
    [SerializeField] private HardcoreConfig hardcoreConfig;
    [SerializeField] private int startingOneUps;
    [SerializeField] private bool hardcoreModeEnabled;

    public event Action<GameState> StateChanged;
    public event Action AutoRetryPopupRequested;
    public event Action ResurrectionStarted;
    public event Action<string> ResurrectionVideoRequested;
    public event Action<string> ResurrectionTipRequested;
    public event Action<bool> ShopOfferOneUpChanged;
    public event Action<IReadOnlyList<HardcoreRemovalType>> HardcoreRemovalChoicesRequested;

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
    private int lastTipIndex = -1;

    private void Start()
    {
        OneUps = Mathf.Max(0, startingOneUps);
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
            RequestHardcoreRemovalChoices();
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

        return true;
    }

    private void RequestHardcoreRemovalChoices()
    {
        if (!IsHardcoreEnabled())
            return;

        if (hardcoreConfig == null || !hardcoreConfig.removePermanentChoiceOnDeath)
            return;

        var options = BuildHardcoreChoices();
        if (options.Count == 0)
            return;

        HardcoreRemovalChoicesRequested?.Invoke(options);
    }

    private List<HardcoreRemovalType> BuildHardcoreChoices()
    {
        var options = new List<HardcoreRemovalType>();
        if (hardcoreConfig == null || hardcoreConfig.choicePool == null || hardcoreConfig.choicePool.Count == 0)
            return options;

        var pool = new List<HardcoreRemovalType>(hardcoreConfig.choicePool);
        var targetCount = Mathf.Clamp(hardcoreConfig.optionsPresentedOnDeath, 1, pool.Count);

        while (options.Count < targetCount && pool.Count > 0)
        {
            var index = UnityEngine.Random.Range(0, pool.Count);
            options.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return options;
    }

    private bool IsHardcoreEnabled()
    {
        return hardcoreModeEnabled && gameOverConfig != null && gameOverConfig.allowHardcoreMode;
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

            var nextTip = GetNextWeightedTip();
            if (!string.IsNullOrWhiteSpace(nextTip))
                ResurrectionTipRequested?.Invoke(nextTip);
        }

        SetState(GameState.Shop);
        ShopOfferOneUpChanged?.Invoke(ForceOfferOneUpInShop());
        resurrectionRoutine = null;
    }

    private string GetNextWeightedTip()
    {
        if (gameOverConfig == null || gameOverConfig.weightedTips == null || gameOverConfig.weightedTips.Count == 0)
            return string.Empty;

        var totalWeight = 0;
        for (var i = 0; i < gameOverConfig.weightedTips.Count; i++)
            totalWeight += Mathf.Max(0, gameOverConfig.weightedTips[i].weight);

        if (totalWeight <= 0)
            return string.Empty;

        for (var attempts = 0; attempts < 8; attempts++)
        {
            var roll = UnityEngine.Random.Range(0, totalWeight);
            var cumulative = 0;
            for (var i = 0; i < gameOverConfig.weightedTips.Count; i++)
            {
                cumulative += Mathf.Max(0, gameOverConfig.weightedTips[i].weight);
                if (roll >= cumulative)
                    continue;

                if (gameOverConfig.weightedTips.Count > 1 && i == lastTipIndex)
                    break;

                lastTipIndex = i;
                return gameOverConfig.weightedTips[i].tip;
            }
        }

        lastTipIndex = (lastTipIndex + 1 + gameOverConfig.weightedTips.Count) % gameOverConfig.weightedTips.Count;
        return gameOverConfig.weightedTips[lastTipIndex].tip;
    }

    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}
