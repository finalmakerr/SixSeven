using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    private const string HardcoreLockedTooltip = "Beat Normal (Level 67) to Unlock";
    private const string IronmanLockedTooltip = "Beat Hardcore (Level 67) to Unlock";

    [Header("Mode Buttons")]
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardcoreButton;
    [SerializeField] private Button ironmanButton;
    [SerializeField] private CanvasGroup hardcoreCanvasGroup;
    [SerializeField] private CanvasGroup ironmanCanvasGroup;
    [SerializeField] private GameObject hardcoreLockIcon;
    [SerializeField] private GameObject ironmanLockIcon;
    [SerializeField] private TMP_Text hardcoreTooltipText;
    [SerializeField] private TMP_Text ironmanTooltipText;

    private void Start()
    {
        ApplyModeButtonStates();
    }

    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void ApplyModeButtonStates()
    {
        var profile = GameManager.Instance != null
            ? GameManager.Instance.Profile
            : null;
        bool hasUnlockedHardcore = profile != null && profile.hasUnlockedHardcore;
        bool hasUnlockedIronman = profile != null && profile.hasUnlockedIronman;

        ApplyButtonState(normalButton, true, null, null, null);
        ApplyButtonState(hardcoreButton, hasUnlockedHardcore, hardcoreCanvasGroup, hardcoreLockIcon, hardcoreTooltipText, HardcoreLockedTooltip);
        ApplyButtonState(ironmanButton, hasUnlockedIronman, ironmanCanvasGroup, ironmanLockIcon, ironmanTooltipText, IronmanLockedTooltip);
    }

    private static void ApplyButtonState(Button button, bool unlocked, CanvasGroup canvasGroup, GameObject lockIcon, TMP_Text tooltipText, string lockedTooltip = "")
    {
        if (button != null)
            button.interactable = unlocked;

        if (canvasGroup != null)
            canvasGroup.alpha = unlocked ? 1f : 0.5f;

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        if (tooltipText != null)
            tooltipText.text = unlocked ? string.Empty : lockedTooltip;
    }

}
