using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("State Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject playingHudPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverTitleText;
    [SerializeField] private Text gameOverSubtitleText;
    [SerializeField] private Image ironmanOverlayTint;
    [SerializeField] private float ironmanShakeDuration = 0.3f;
    [SerializeField] private float ironmanShakeIntensity = 6f;

    [Header("Death Flow")]
    [SerializeField] private GameObject retryPopupPanel;
    [SerializeField] private float retryPopupDuration = 1.5f;
    [SerializeField] private CanvasGroup deathFadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private VideoPlayer resurrectionVideoPlayer;
    [SerializeField] private Text tipText;

    private GameManager gameManager;
    private Coroutine retryPopupRoutine;
    private Coroutine fadeRoutine;
    private Coroutine ironmanShakeRoutine;
    private string defaultGameOverTitle;
    private string defaultGameOverSubtitle;
    private Vector3 ironmanShakeOriginalPosition;
    private bool hasIronmanShakeOriginalPosition;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameOverTitleText != null)
            defaultGameOverTitle = gameOverTitleText.text;

        if (gameOverSubtitleText != null)
            defaultGameOverSubtitle = gameOverSubtitleText.text;
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
            gameManager.AutoRetryPopupRequested += ShowRetryPopup;
            gameManager.ResurrectionStarted += HandleResurrectionStarted;
            gameManager.ResurrectionVideoRequested += HandleResurrectionVideoRequested;
            gameManager.ResurrectionTipRequested += HandleResurrectionTipRequested;
            HandleStateChanged(gameManager.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.StateChanged -= HandleStateChanged;
            gameManager.AutoRetryPopupRequested -= ShowRetryPopup;
            gameManager.ResurrectionStarted -= HandleResurrectionStarted;
            gameManager.ResurrectionVideoRequested -= HandleResurrectionVideoRequested;
            gameManager.ResurrectionTipRequested -= HandleResurrectionTipRequested;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        SetPanelActive(mainMenuPanel, state == GameState.MainMenu);
        SetPanelActive(loadingPanel, state == GameState.Loading);
        SetPanelActive(playingHudPanel, state == GameState.Playing);
        SetPanelActive(shopPanel, state == GameState.Shop);
        SetPanelActive(levelCompletePanel, state == GameState.LevelComplete);
        SetPanelActive(gameOverPanel, state == GameState.GameOver);

        if (state == GameState.GameOver)
            ApplyGameOverTheme();

        if (state == GameState.Shop)
            FadeTo(0f);
    }

    private void ApplyGameOverTheme()
    {
        bool isIronman = gameManager != null && gameManager.CurrentGameMode == GameMode.Ironman;

        if (gameOverTitleText != null)
            gameOverTitleText.text = isIronman ? "IRONMAN RUN FAILED" : defaultGameOverTitle;

        if (gameOverSubtitleText != null)
            gameOverSubtitleText.text = isIronman ? "No second chances." : defaultGameOverSubtitle;

        if (ironmanOverlayTint != null)
            ironmanOverlayTint.enabled = isIronman;

        if (!isIronman)
        {
            if (ironmanShakeRoutine != null)
            {
                StopCoroutine(ironmanShakeRoutine);
                ironmanShakeRoutine = null;
            }

            if (hasIronmanShakeOriginalPosition && gameOverPanel != null)
                gameOverPanel.transform.localPosition = ironmanShakeOriginalPosition;

            hasIronmanShakeOriginalPosition = false;
            return;
        }

        if (ironmanShakeRoutine != null)
            StopCoroutine(ironmanShakeRoutine);

        ironmanShakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        if (gameOverPanel == null)
        {
            ironmanShakeRoutine = null;
            yield break;
        }

        var panelTransform = gameOverPanel.transform;
        var originalPosition = panelTransform.localPosition;
        ironmanShakeOriginalPosition = originalPosition;
        hasIronmanShakeOriginalPosition = true;

        var duration = Mathf.Max(0f, ironmanShakeDuration);
        var intensity = Mathf.Max(0f, ironmanShakeIntensity);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var randomOffset = Random.insideUnitCircle * intensity;
            panelTransform.localPosition = originalPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
            yield return null;
        }

        panelTransform.localPosition = originalPosition;
        ironmanShakeRoutine = null;
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

    private void HandleResurrectionStarted()
    {
        FadeTo(1f);
    }

    private void HandleResurrectionVideoRequested(string videoPath)
    {
        if (resurrectionVideoPlayer == null)
            return;

        if (string.IsNullOrWhiteSpace(videoPath))
            return;

        resurrectionVideoPlayer.source = VideoSource.Url;
        resurrectionVideoPlayer.url = videoPath;
        resurrectionVideoPlayer.Play();
    }

    private void HandleResurrectionTipRequested(string tip)
    {
        if (tipText == null)
            return;

        tipText.text = tip;
    }

    private void FadeTo(float targetAlpha)
    {
        if (deathFadeCanvasGroup == null)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha)
    {
        var start = deathFadeCanvasGroup.alpha;
        var duration = Mathf.Max(0.01f, fadeDuration);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            deathFadeCanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        deathFadeCanvasGroup.alpha = targetAlpha;
        fadeRoutine = null;
    }
}
