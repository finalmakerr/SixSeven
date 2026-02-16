using UnityEngine;

public enum GameMode
{
    Normal,
    Hardcore,
    Ironman
}

public class ModeAuraController : MonoBehaviour
{
    [SerializeField] private GameObject normalAura;
    [SerializeField] private GameObject hardcoreAura;
    [SerializeField] private GameObject ironmanAura;

    public void ApplyModeAura(GameMode mode)
    {
        if (normalAura != null)
            normalAura.SetActive(false);

        if (hardcoreAura != null)
            hardcoreAura.SetActive(false);

        if (ironmanAura != null)
            ironmanAura.SetActive(false);

        switch (mode)
        {
            case GameMode.Hardcore:
                if (hardcoreAura != null)
                    hardcoreAura.SetActive(true);
                break;

            case GameMode.Ironman:
                if (ironmanAura != null)
                    ironmanAura.SetActive(true);
                break;

            default:
                if (normalAura != null)
                    normalAura.SetActive(true);
                break;
        }
    }
}
