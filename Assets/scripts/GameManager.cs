using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float loadingDuration = 0.75f;

    [Header("Death Flow")]
    [SerializeField] private bool allowDeathAutoRetry = true;

    public event Action<GameState> StateChanged;
    public event Action AutoRetryPopupRequested;

    /// <summary>
    /// Hook this from your level/gameplay systems.
    /// Return true when auto-retry should happen (e.g., player has 3 stars).
    /// The hook is also responsible for consuming/resetting those stars.
    /// </summary>
    public Func<bool> TryAutoRetryOnDeath;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    private Coroutine loadingRoutine;

    private void Start()
    {
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

        if (allowDeathAutoRetry && TryAutoRetryOnDeath?.Invoke() == true)
        {
            AutoRetryPopupRequested?.Invoke();
            StartLoadingFromCurrentLevel();
            return;
        }

        SetState(GameState.GameOver);
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

    private void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}
