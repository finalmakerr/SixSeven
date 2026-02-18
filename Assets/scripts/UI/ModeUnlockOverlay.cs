using System.Collections;
using TMPro;
using UnityEngine;

public class ModeUnlockOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlayCanvas;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text modeNameText;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockClip;

    private Coroutine displayRoutine;

    private void OnEnable()
    {
        GameManager.OnModeUnlocked += HandleModeUnlocked;
    }

    private void OnDisable()
    {
        GameManager.OnModeUnlocked -= HandleModeUnlocked;

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }
    }

    private void HandleModeUnlocked(GameMode mode)
    {
        if (!isActiveAndEnabled || overlayCanvas == null)
            return;

        TriggerManual(mode);
    }

    public void TriggerManual(GameMode mode)
    {
        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        displayRoutine = StartCoroutine(ShowOverlayRoutine(mode));
    }

    private IEnumerator ShowOverlayRoutine(GameMode mode)
    {
        gameObject.SetActive(true);
        overlayCanvas.gameObject.SetActive(true);
        overlayCanvas.alpha = 0f;

        if (titleText != null)
            titleText.text = "NEW MODE UNLOCKED";

        if (modeNameText != null)
            modeNameText.text = mode.ToString().ToUpperInvariant();

        if (audioSource != null && unlockClip != null)
            audioSource.PlayOneShot(unlockClip);

        yield return FadeAlpha(0f, 1f, 0.3f);

        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        yield return FadeAlpha(1f, 0f, 0.3f);

        overlayCanvas.gameObject.SetActive(false);
        displayRoutine = null;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (overlayCanvas == null)
            yield break;

        if (duration <= 0f)
        {
            overlayCanvas.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        overlayCanvas.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlayCanvas.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        overlayCanvas.alpha = to;
    }
}
