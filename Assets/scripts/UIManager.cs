using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("State Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject playingHudPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Death Flow")]
    [SerializeField] private GameObject retryPopupPanel;
    [SerializeField] private float retryPopupDuration = 1.5f;

    private GameManager gameManager;
    private Coroutine retryPopupRoutine;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
            gameManager.AutoRetryPopupRequested += ShowRetryPopup;
            HandleStateChanged(gameManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= HandleStateChanged;
            gameManager.AutoRetryPopupRequested -= ShowRetryPopup;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        SetPanelActive(mainMenuPanel, state == GameState.MainMenu);
        SetPanelActive(loadingPanel, state == GameState.Loading);
        SetPanelActive(playingHudPanel, state == GameState.Playing);
        SetPanelActive(levelCompletePanel, state == GameState.LevelComplete);
        SetPanelActive(gameOverPanel, state == GameState.GameOver);
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null && panel.activeSelf != isActive)
            panel.SetActive(isActive);
    }

    public void OnPlayPressed()
    {
        if (gameManager != null)
            gameManager.StartGame();
    }

    public void OnRetryPressed()
    {
        if (gameManager != null)
            gameManager.RetryLevel();
    }

    public void OnReturnToMenuPressed()
    {
        if (gameManager != null)
            gameManager.ReturnToMainMenu();
    }

    public void OnNextLevelPressed()
    {
        if (gameManager != null)
            gameManager.StartGame();
    }

    private void ShowRetryPopup()
    {
        if (retryPopupPanel == null)
            return;

        if (retryPopupRoutine != null)
            StopCoroutine(retryPopupRoutine);

        retryPopupRoutine = StartCoroutine(ShowRetryPopupRoutine());
    }

    private System.Collections.IEnumerator ShowRetryPopupRoutine()
    {
        retryPopupPanel.SetActive(true);

        if (retryPopupDuration > 0f)
            yield return new WaitForSeconds(retryPopupDuration);

        retryPopupPanel.SetActive(false);
        retryPopupRoutine = null;
    }
}
