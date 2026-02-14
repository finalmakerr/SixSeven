using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixSeven.Systems
{
    /// <summary>
    /// Controls runtime-only spell icon feedback states without requiring variant sprite assets.
    /// State precedence: Locked > Cooldown > Disabled > Hover > Normal.
    /// </summary>
    public class SpellIconUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private enum VisualState
        {
            Normal,
            Hover,
            Disabled,
            Cooldown,
            Locked
        }

        [Header("Main")]
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform iconTransform;
        [SerializeField] private Material runtimeMaterialTemplate;

        [Header("Optional Highlights")]
        [SerializeField] private Graphic glowGraphic;
        [SerializeField] private Graphic outlineGraphic;

        [Header("Overlays")]
        [SerializeField] private Image darkOverlay;
        [SerializeField] private Image cooldownRadialMask;
        [SerializeField] private Image lockOverlay;

        [Header("Cooldown Text")]
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private bool showCooldownText = true;

        [Header("State Styling")]
        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float disabledScale = 1f;
        [SerializeField] private float cooldownScale = 1f;
        [SerializeField] private float lockedScale = 0.96f;
        [SerializeField] private float normalSaturation = 1f;
        [SerializeField] private float hoverSaturation = 1.15f;
        [SerializeField] private float disabledSaturation = 0.2f;
        [SerializeField] private float cooldownSaturation = 0.65f;
        [SerializeField] private float lockedSaturation = 0f;
        [SerializeField] private float normalBrightness = 1f;
        [SerializeField] private float hoverBrightness = 1.2f;
        [SerializeField] private float disabledBrightness = 0.6f;
        [SerializeField] private float cooldownBrightness = 0.85f;
        [SerializeField] private float lockedBrightness = 0.5f;

        [Header("Animation")]
        [SerializeField] private float tweenDuration = 0.12f;
        [SerializeField] private float readyPopDuration = 0.2f;
        [SerializeField] private float readyPopScale = 1.16f;
        [SerializeField] private float readyBrightnessSpike = 1.45f;

        [Header("Insufficient Energy Feedback")]
        [SerializeField] private Color insufficientEnergyFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private float insufficientFlashDuration = 0.12f;
        [SerializeField] private float insufficientShakeDuration = 0.18f;
        [SerializeField] private float insufficientShakeMagnitude = 6f;

        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

        private Material runtimeMaterial;
        private Vector3 baseLocalScale;
        private Vector2 baseAnchoredPosition;

        private Coroutine stateTweenRoutine;
        private Coroutine cooldownRoutine;
        private Coroutine insufficientRoutine;

        private bool isHovering;
        private bool isDisabled;
        private bool isLocked;
        private bool isCooldownActive;
        private float cooldownRemaining;

        public bool IsLocked => isLocked;
        public bool IsDisabled => isDisabled;
        public bool IsCooldownActive => isCooldownActive;

        private void Awake()
        {
            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }

            if (iconTransform == null && iconImage != null)
            {
                iconTransform = iconImage.rectTransform;
            }

            baseLocalScale = iconTransform != null ? iconTransform.localScale : Vector3.one;
            baseAnchoredPosition = iconTransform != null ? iconTransform.anchoredPosition : Vector2.zero;

            SetupRuntimeMaterial();
            ForceApplyStateImmediate();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            stateTweenRoutine = null;
            cooldownRoutine = null;
            insufficientRoutine = null;

            if (iconTransform != null)
            {
                iconTransform.anchoredPosition = baseAnchoredPosition;
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        public void SetHover(bool value)
        {
            isHovering = value;
            ApplyResolvedState();
        }

        public void SetDisabled(bool value)
        {
            isDisabled = value;
            ApplyResolvedState();
        }

        public void SetLocked(bool value)
        {
            isLocked = value;
            ApplyResolvedState();
        }

        public void SetSelected(bool value)
        {
            isHovering = value;
            ApplyResolvedState();
        }

        public void StartCooldown(float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                StopCooldown(false);
                return;
            }

            if (cooldownRoutine != null)
            {
                StopCoroutine(cooldownRoutine);
            }

            cooldownRoutine = StartCoroutine(CooldownRoutine(durationSeconds));
        }

        public void StopCooldown(bool playReadyPop = true)
        {
            if (cooldownRoutine != null)
            {
                StopCoroutine(cooldownRoutine);
                cooldownRoutine = null;
            }

            isCooldownActive = false;
            cooldownRemaining = 0f;
            ApplyResolvedState();

            if (playReadyPop)
            {
                StartCoroutine(ReadyPopRoutine());
            }
        }

        public void PlayInsufficientEnergyFeedback()
        {
            if (insufficientRoutine != null)
            {
                StopCoroutine(insufficientRoutine);
            }

            insufficientRoutine = StartCoroutine(InsufficientEnergyRoutine());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHover(false);
        }

        private void SetupRuntimeMaterial()
        {
            if (iconImage == null)
            {
                return;
            }

            var source = runtimeMaterialTemplate != null ? runtimeMaterialTemplate : iconImage.material;
            if (source == null)
            {
                return;
            }

            runtimeMaterial = new Material(source);
            runtimeMaterial.name = $"{source.name}_Runtime_{name}";
            iconImage.material = runtimeMaterial;
        }

        private IEnumerator CooldownRoutine(float duration)
        {
            isCooldownActive = true;
            cooldownRemaining = duration;
            ApplyResolvedState();

            while (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.unscaledDeltaTime;
                UpdateCooldownOverlay(Mathf.Clamp01(cooldownRemaining / duration), Mathf.Max(0f, cooldownRemaining));
                yield return null;
            }

            cooldownRemaining = 0f;
            isCooldownActive = false;
            cooldownRoutine = null;
            ApplyResolvedState();
            yield return ReadyPopRoutine();
        }

        private IEnumerator ReadyPopRoutine()
        {
            if (iconTransform == null)
            {
                yield break;
            }

            var halfDuration = Mathf.Max(0.01f, readyPopDuration * 0.5f);
            var originalScale = iconTransform.localScale;

            yield return AnimateScaleAndBrightness(originalScale, baseLocalScale * readyPopScale, GetResolvedBrightness() * readyBrightnessSpike, halfDuration);
            yield return AnimateScaleAndBrightness(iconTransform.localScale, GetResolvedScale(), GetResolvedBrightness(), halfDuration);
        }

        private IEnumerator AnimateScaleAndBrightness(Vector3 fromScale, Vector3 toScale, float toBrightness, float duration)
        {
            var elapsed = 0f;
            var startBrightness = GetCurrentBrightness();

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);

                if (iconTransform != null)
                {
                    iconTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                }

                SetMaterialBrightness(Mathf.LerpUnclamped(startBrightness, toBrightness, eased));
                yield return null;
            }

            if (iconTransform != null)
            {
                iconTransform.localScale = toScale;
            }

            SetMaterialBrightness(toBrightness);
        }

        private IEnumerator InsufficientEnergyRoutine()
        {
            if (iconImage == null || iconTransform == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var originalColor = iconImage.color;
            var originalPosition = iconTransform.anchoredPosition;

            while (elapsed < insufficientShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var flashT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, insufficientFlashDuration));
                var flashBlend = 1f - Mathf.Abs((flashT * 2f) - 1f);
                iconImage.color = Color.Lerp(originalColor, insufficientEnergyFlashColor, flashBlend);

                var shakeFalloff = 1f - Mathf.Clamp01(elapsed / insufficientShakeDuration);
                var shakeOffset = Mathf.Sin(elapsed * 90f) * insufficientShakeMagnitude * shakeFalloff;
                iconTransform.anchoredPosition = originalPosition + new Vector2(shakeOffset, 0f);
                yield return null;
            }

            iconImage.color = originalColor;
            iconTransform.anchoredPosition = originalPosition;
            insufficientRoutine = null;
            ApplyResolvedState();
        }

        private void ApplyResolvedState()
        {
            var resolved = ResolveState();
            var targetScale = GetScaleForState(resolved);
            var targetSaturation = GetSaturationForState(resolved);
            var targetBrightness = GetBrightnessForState(resolved);

            var showDarkOverlay = resolved is VisualState.Disabled or VisualState.Locked;
            var showCooldown = resolved == VisualState.Cooldown;
            var showLock = resolved == VisualState.Locked;
            var showGlow = resolved == VisualState.Hover;

            if (stateTweenRoutine != null)
            {
                StopCoroutine(stateTweenRoutine);
            }

            stateTweenRoutine = StartCoroutine(StateTweenRoutine(targetScale, targetSaturation, targetBrightness));

            SetGraphicVisible(darkOverlay, showDarkOverlay);
            SetGraphicVisible(cooldownRadialMask, showCooldown);
            SetGraphicVisible(lockOverlay, showLock);
            SetGraphicVisible(glowGraphic, showGlow);
            SetGraphicVisible(outlineGraphic, showGlow);

            if (showCooldown)
            {
                var currentFill = cooldownRadialMask != null ? cooldownRadialMask.fillAmount : 0f;
                UpdateCooldownOverlay(cooldownRemaining <= 0f ? 0f : currentFill, cooldownRemaining);
            }
            else
            {
                UpdateCooldownOverlay(0f, 0f);
            }
        }

        private void ForceApplyStateImmediate()
        {
            var state = ResolveState();
            if (iconTransform != null)
            {
                iconTransform.localScale = GetScaleForState(state);
            }

            SetMaterialSaturation(GetSaturationForState(state));
            SetMaterialBrightness(GetBrightnessForState(state));
            ApplyResolvedState();
        }

        private IEnumerator StateTweenRoutine(Vector3 targetScale, float targetSaturation, float targetBrightness)
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, tweenDuration);
            var startScale = iconTransform != null ? iconTransform.localScale : Vector3.one;
            var startSaturation = GetCurrentSaturation();
            var startBrightness = GetCurrentBrightness();

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));

                if (iconTransform != null)
                {
                    iconTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                }

                SetMaterialSaturation(Mathf.LerpUnclamped(startSaturation, targetSaturation, t));
                SetMaterialBrightness(Mathf.LerpUnclamped(startBrightness, targetBrightness, t));
                yield return null;
            }

            if (iconTransform != null)
            {
                iconTransform.localScale = targetScale;
            }

            SetMaterialSaturation(targetSaturation);
            SetMaterialBrightness(targetBrightness);
            stateTweenRoutine = null;
        }

        private VisualState ResolveState()
        {
            if (isLocked)
            {
                return VisualState.Locked;
            }

            if (isCooldownActive)
            {
                return VisualState.Cooldown;
            }

            if (isDisabled)
            {
                return VisualState.Disabled;
            }

            if (isHovering)
            {
                return VisualState.Hover;
            }

            return VisualState.Normal;
        }

        private Vector3 GetResolvedScale() => GetScaleForState(ResolveState());
        private float GetResolvedBrightness() => GetBrightnessForState(ResolveState());

        private Vector3 GetScaleForState(VisualState state)
        {
            var scaleMultiplier = state switch
            {
                VisualState.Hover => hoverScale,
                VisualState.Disabled => disabledScale,
                VisualState.Cooldown => cooldownScale,
                VisualState.Locked => lockedScale,
                _ => normalScale
            };

            return baseLocalScale * Mathf.Clamp(scaleMultiplier, 0.01f, 5f);
        }

        private float GetSaturationForState(VisualState state)
        {
            return state switch
            {
                VisualState.Hover => hoverSaturation,
                VisualState.Disabled => disabledSaturation,
                VisualState.Cooldown => cooldownSaturation,
                VisualState.Locked => lockedSaturation,
                _ => normalSaturation
            };
        }

        private float GetBrightnessForState(VisualState state)
        {
            return state switch
            {
                VisualState.Hover => hoverBrightness,
                VisualState.Disabled => disabledBrightness,
                VisualState.Cooldown => cooldownBrightness,
                VisualState.Locked => lockedBrightness,
                _ => normalBrightness
            };
        }

        private void UpdateCooldownOverlay(float fillAmount, float remaining)
        {
            if (cooldownRadialMask != null)
            {
                cooldownRadialMask.fillMethod = Image.FillMethod.Radial360;
                cooldownRadialMask.fillOrigin = (int)Image.Origin360.Top;
                cooldownRadialMask.fillClockwise = false;
                cooldownRadialMask.fillAmount = Mathf.Clamp01(fillAmount);
            }

            if (cooldownText != null)
            {
                var enabled = showCooldownText && isCooldownActive;
                cooldownText.gameObject.SetActive(enabled);
                if (enabled)
                {
                    cooldownText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
                }
            }
        }

        private void SetGraphicVisible(Graphic graphic, bool visible)
        {
            if (graphic != null)
            {
                graphic.gameObject.SetActive(visible);
            }
        }

        private float GetCurrentSaturation()
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(SaturationId))
            {
                return runtimeMaterial.GetFloat(SaturationId);
            }

            return normalSaturation;
        }

        private float GetCurrentBrightness()
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(BrightnessId))
            {
                return runtimeMaterial.GetFloat(BrightnessId);
            }

            return normalBrightness;
        }

        private void SetMaterialSaturation(float value)
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(SaturationId))
            {
                runtimeMaterial.SetFloat(SaturationId, value);
            }
        }

        private void SetMaterialBrightness(float value)
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(BrightnessId))
            {
                runtimeMaterial.SetFloat(BrightnessId, value);
            }
        }

        private static float EaseOutCubic(float t)
        {
            var inv = 1f - Mathf.Clamp01(t);
            return 1f - (inv * inv * inv);
        }
    }
}
