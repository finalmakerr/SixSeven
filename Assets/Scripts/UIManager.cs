using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("State Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject playingHudPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject gameOverPanel;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
            HandleStateChanged(gameManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleStateChanged;
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
}
