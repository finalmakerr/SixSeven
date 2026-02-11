using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SixSeven.Systems
{
    /// <summary>
    /// Renders HP and Energy icons as horizontally growing bars.
    /// Bars only increase slot count, never shrink it.
    /// </summary>
    public class VitalsBarUIController : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private RectTransform hpBarRoot;
        [SerializeField] private Image hpIconPrefab;

        [Header("Energy")]
        [SerializeField] private RectTransform energyBarRoot;
        [SerializeField] private Image energyIconPrefab;

        [Header("Sprites")]
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite halfSprite;
        [SerializeField] private Sprite fullSprite;
        [SerializeField] private Sprite shieldOverlaySprite;

        private readonly List<Image> hpIcons = new List<Image>();
        private readonly List<Image> energyIcons = new List<Image>();
        private readonly List<Image> hpShieldOverlays = new List<Image>();

        private int maxHpHeartsShown;
        private int maxEnergyHeartsShown;

        public void Refresh(PlayerVitalsSystem vitals)
        {
            if (vitals == null)
            {
                return;
            }

            // Expand only when maximum increases.
            maxHpHeartsShown = Mathf.Max(maxHpHeartsShown, vitals.UnlockedHearts);
            maxEnergyHeartsShown = Mathf.Max(maxEnergyHeartsShown, vitals.EnergyUnlockedHearts);

            EnsureIconCount(hpBarRoot, hpIconPrefab, hpIcons, maxHpHeartsShown);
            EnsureIconCount(energyBarRoot, energyIconPrefab, energyIcons, maxEnergyHeartsShown);
            EnsureShieldOverlayCount(maxHpHeartsShown);

            ApplyHalfHeartStates(hpIcons, maxHpHeartsShown, vitals.CurrentHp, vitals.UnlockedHearts);
            ApplyHalfHeartStates(energyIcons, maxEnergyHeartsShown, vitals.CurrentEnergy, vitals.EnergyUnlockedHearts);
            ApplyShieldState(vitals.CurrentShield, vitals.UnlockedHearts);
        }

        private void EnsureIconCount(RectTransform root, Image prefab, List<Image> cache, int requiredCount)
        {
            if (root == null || prefab == null)
            {
                return;
            }

            while (cache.Count < requiredCount)
            {
                cache.Add(Instantiate(prefab, root));
            }
        }

        private void EnsureShieldOverlayCount(int requiredCount)
        {
            if (shieldOverlaySprite == null)
            {
                return;
            }

            for (var i = hpShieldOverlays.Count; i < requiredCount && i < hpIcons.Count; i++)
            {
                var overlayObject = new GameObject($"ShieldOverlay_{i}", typeof(RectTransform), typeof(Image));
                overlayObject.transform.SetParent(hpIcons[i].transform, false);

                var overlayImage = overlayObject.GetComponent<Image>();
                overlayImage.raycastTarget = false;
                overlayImage.sprite = shieldOverlaySprite;
                overlayImage.enabled = false;

                var rect = overlayImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                hpShieldOverlays.Add(overlayImage);
            }
        }

        private void ApplyHalfHeartStates(List<Image> icons, int shownHearts, int filledUnits, int unlockedHearts)
        {
            for (var i = 0; i < icons.Count; i++)
            {
                var heartStartUnit = i * PlayerVitalsSystem.HpPerHeart;
                var unitsInHeart = Mathf.Clamp(filledUnits - heartStartUnit, 0, PlayerVitalsSystem.HpPerHeart);

                var sprite = unitsInHeart switch
                {
                    2 => fullSprite,
                    1 => halfSprite,
                    _ => emptySprite
                };

                icons[i].sprite = sprite;
                icons[i].enabled = i < shownHearts;

                // Keep layout width stable for previously reached sizes.
                if (i >= unlockedHearts)
                {
                    var color = icons[i].color;
                    color.a = 0.35f;
                    icons[i].color = color;
                }
                else
                {
                    var color = icons[i].color;
                    color.a = 1f;
                    icons[i].color = color;
                }
            }
        }

        private void ApplyShieldState(int shieldUnits, int unlockedHearts)
        {
            for (var i = 0; i < hpShieldOverlays.Count; i++)
            {
                var hasShieldLayer = i < shieldUnits;
                hpShieldOverlays[i].enabled = i < unlockedHearts && hasShieldLayer;
            }
        }
    }
}
