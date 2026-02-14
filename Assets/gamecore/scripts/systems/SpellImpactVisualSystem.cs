using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace GameCore
{
    [Serializable]
    public struct SpellImpactRequest
    {
        public string SchoolKey;
        public Vector3 WorldPosition;
        public int Tier;
        public bool TriggerButtonFeedback;
    }

    public sealed class SpellImpactVisualSystem : MonoBehaviour
    {
        [Header("Pool")]
        [SerializeField] private SpriteRenderer overlayPrefab;
        [SerializeField] private SpriteRenderer groundPulsePrefab;
        [SerializeField, Min(1)] private int overlayPoolSize = 24;
        [SerializeField, Min(1)] private int groundPulsePoolSize = 12;

        [Header("Addressables")]
        [SerializeField] private string defaultSchoolKey = "_default";
        [SerializeField] private string[] preloadSchoolKeys = { "fire", "shadow", "holy" };

        [Header("Animation")]
        [SerializeField] private AnimationCurve normalScaleCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.15f);
        [SerializeField] private AnimationCurve importantScaleCurve = AnimationCurve.EaseInOut(0f, 0.65f, 1f, 1.45f);
        [SerializeField] private float normalDuration = 0.28f;
        [SerializeField] private float importantDuration = 0.36f;
        [SerializeField] private float normalFreezeDuration = 0.025f;
        [SerializeField] private float importantFreezeDuration = 0.06f;
        [SerializeField] private Vector2 randomScaleVariance = new Vector2(0.92f, 1.08f);
        [SerializeField] private Vector2 randomRotationVariance = new Vector2(-18f, 18f);

        [Header("Feedback")]
        [SerializeField] private AudioSource impactAudioSource;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private string impactAudioKey = "sfx/spell-impact";
        [SerializeField] private float tierTwoVolumeMultiplier = 1.2f;

        [Header("Scene Assets")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private float tierOneButtonPopScale = 1.08f;
        [SerializeField] private float tierTwoButtonPopScale = 1.16f;
        [SerializeField] private float buttonPopDuration = 0.12f;

        [Header("Camera Shake")]
        [SerializeField] private AnimationCurve normalShakeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.12f);
        [SerializeField] private AnimationCurve importantShakeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.3f);

        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> inFlightLoads = new Dictionary<string, AsyncOperationHandle<Sprite>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> completedHandles = new Dictionary<string, AsyncOperationHandle<Sprite>>(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<SpriteRenderer> overlayPool = new Queue<SpriteRenderer>();
        private readonly Queue<SpriteRenderer> groundPulsePool = new Queue<SpriteRenderer>();
        private readonly HashSet<SpriteRenderer> activeOverlays = new HashSet<SpriteRenderer>();
        private readonly HashSet<SpriteRenderer> activeGroundPulses = new HashSet<SpriteRenderer>();

        private void Awake()
        {
            BuildPool(overlayPrefab, overlayPoolSize, overlayPool);
            BuildPool(groundPulsePrefab, groundPulsePoolSize, groundPulsePool);

            if (audioService == null)
            {
                audioService = AudioService.Instance;
            }
        }

        private void Start()
        {
            _ = StartCoroutine(PreloadRoutine());
        }

        public void PlayImpact(SpellImpactRequest request)
        {
            var overlay = RentOverlay();
            if (overlay == null)
            {
                return;
            }

            ConfigureOverlayTransform(overlay.transform, request.WorldPosition);
            overlay.sprite = null;

            var key = string.IsNullOrWhiteSpace(request.SchoolKey) ? defaultSchoolKey : request.SchoolKey.Trim().ToLowerInvariant();
            _ = StartCoroutine(AssignSpriteWhenReady(overlay, key));
            _ = StartCoroutine(PlayOverlayRoutine(overlay, request.Tier >= 2));

            if (request.TriggerButtonFeedback)
            {
                // Button selection is done by the caller. This system only applies art + animation intensity.
            }
        }

        public void ApplyButtonFeedback(Image targetImage, string schoolKey, bool importantTier)
        {
            if (targetImage == null)
            {
                return;
            }

            var key = string.IsNullOrWhiteSpace(schoolKey) ? defaultSchoolKey : schoolKey.Trim().ToLowerInvariant();
            _ = StartCoroutine(AssignButtonSpriteWhenReady(targetImage, key));
            _ = StartCoroutine(ButtonPopRoutine(targetImage.rectTransform, importantTier));
        }
        private IEnumerator PreloadRoutine()
        {
            yield return LoadSpriteWithFallback(defaultSchoolKey);

            if (preloadSchoolKeys == null)
            {
                yield break;
            }

            for (var i = 0; i < preloadSchoolKeys.Length; i++)
            {
                var key = preloadSchoolKeys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                yield return LoadSpriteWithFallback(key.Trim().ToLowerInvariant());
            }
        }

        private IEnumerator AssignSpriteWhenReady(SpriteRenderer targetRenderer, string key)
        {
            if (targetRenderer == null)
            {
                yield break;
            }

            yield return LoadSpriteWithFallback(key);
            if (targetRenderer == null)
            {
                yield break;
            }

            if (spriteCache.TryGetValue(key, out var sprite) && sprite != null)
            {
                targetRenderer.sprite = sprite;
                yield break;
            }

            if (spriteCache.TryGetValue(defaultSchoolKey, out var fallback) && fallback != null)
            {
                targetRenderer.sprite = fallback;
            }
        }

        private IEnumerator AssignButtonSpriteWhenReady(Image image, string key)
        {
            yield return LoadSpriteWithFallback(key);

            if (image == null)
            {
                yield break;
            }

            if (spriteCache.TryGetValue(key, out var sprite) && sprite != null)
            {
                image.sprite = sprite;
                yield break;
            }

            if (spriteCache.TryGetValue(defaultSchoolKey, out var fallback) && fallback != null)
            {
                image.sprite = fallback;
            }
        }

        // Addressables integration notes:
        // - Cache loaded sprites for hot paths (cascades, multi-hit spells).
        // - Track in-flight requests to avoid duplicate Addressables.LoadAssetAsync calls.
        // - Keep completed handles and release them during OnDestroy to avoid leaks.
        private IEnumerator LoadSpriteWithFallback(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = defaultSchoolKey;
            }

            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
            {
                yield break;
            }

            yield return LoadSpriteInternal(key);

            if ((!spriteCache.TryGetValue(key, out cached) || cached == null) && !string.Equals(key, defaultSchoolKey, StringComparison.OrdinalIgnoreCase))
            {
                yield return LoadSpriteInternal(defaultSchoolKey);
                if (spriteCache.TryGetValue(defaultSchoolKey, out var fallback) && fallback != null)
                {
                    spriteCache[key] = fallback;
                }
            }
        }

        private IEnumerator LoadSpriteInternal(string key)
        {
            if (spriteCache.TryGetValue(key, out var cached) && cached != null)
            {
                yield break;
            }

            if (inFlightLoads.TryGetValue(key, out var inFlight))
            {
                yield return inFlight;
                yield break;
            }

            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            inFlightLoads[key] = handle;
            yield return handle;
            inFlightLoads.Remove(key);

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                spriteCache[key] = handle.Result;
                completedHandles[key] = handle;
                yield break;
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning($"SpellImpactVisualSystem: Addressable sprite load failed for key '{key}'.", this);
#endif
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        private IEnumerator PlayOverlayRoutine(SpriteRenderer overlay, bool importantTier)
        {
            if (overlay == null)
            {
                yield break;
            }

            var duration = importantTier ? importantDuration : normalDuration;
            var freezeDuration = importantTier ? importantFreezeDuration : normalFreezeDuration;
            var scaleCurve = importantTier ? importantScaleCurve : normalScaleCurve;
            var shakeCurve = importantTier ? importantShakeCurve : normalShakeCurve;

            var baseScale = Vector3.one * UnityEngine.Random.Range(randomScaleVariance.x, randomScaleVariance.y);
            var zRotation = UnityEngine.Random.Range(randomRotationVariance.x, randomRotationVariance.y);
            overlay.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

            var peakTriggered = false;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var curveValue = Mathf.Max(0f, scaleCurve.Evaluate(t));
                overlay.transform.localScale = baseScale * curveValue;

                if (!peakTriggered && t >= 0.5f)
                {
                    peakTriggered = true;
                    TriggerImpactPeakFeedback(importantTier, shakeCurve);
                    yield return StartCoroutine(CascadeSafeFreeze(freezeDuration));
                }

                yield return null;
            }

            if (importantTier)
            {
                var pulse = RentGroundPulse();
                if (pulse != null)
                {
                    pulse.transform.position = overlay.transform.position;
                    pulse.sprite = overlay.sprite;
                    _ = StartCoroutine(PlayGroundPulseRoutine(pulse));
                }
            }

            ReturnOverlay(overlay);
        }

        private IEnumerator PlayGroundPulseRoutine(SpriteRenderer pulse)
        {
            if (pulse == null)
            {
                yield break;
            }

            const float duration = 0.22f;
            const float endScale = 1.35f;
            var elapsed = 0f;
            var color = pulse.color;
            color.a = 0.85f;
            pulse.color = color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                pulse.transform.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one * endScale, t);
                color.a = Mathf.Lerp(0.85f, 0f, t);
                pulse.color = color;
                yield return null;
            }

            ReturnGroundPulse(pulse);
        }

        private void TriggerImpactPeakFeedback(bool importantTier, AnimationCurve shakeCurve)
        {
            PlayImpactSound(importantTier);

            // Existing camera shake ownership remains external; this call is the hook point.
            // The curve is preserved as a parameter for current game shake integration.
            _ = shakeCurve;
        }

        private void PlayImpactSound(bool importantTier)
        {
            if (impactAudioSource == null)
            {
                return;
            }

            var volume = importantTier ? tierTwoVolumeMultiplier : 1f;

            if (audioService == null)
            {
                audioService = AudioService.Instance;
            }

            if (audioService != null)
            {
                audioService.play_sfx(impactAudioKey, volume);
                return;
            }

            if (impactClip == null)
            {
                Debug.LogWarning($"SpellImpactVisualSystem: No clip available for key '{impactAudioKey}'. Skipping playback.", this);
                return;
            }

            impactAudioSource.PlayOneShot(impactClip, volume);
        }

        private static IEnumerator CascadeSafeFreeze(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            // Uses realtime waiting so overlapping cascades can safely finish their own freeze windows.
            var originalScale = Time.timeScale;
            Time.timeScale = Mathf.Min(Time.timeScale, 0.0001f);
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalScale;
        }

        private IEnumerator ButtonPopRoutine(RectTransform target, bool importantTier)
        {
            if (target == null)
            {
                yield break;
            }

            var start = Vector3.one;
            var peak = Vector3.one * (importantTier ? tierTwoButtonPopScale : tierOneButtonPopScale);
            var elapsed = 0f;

            while (elapsed < buttonPopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / buttonPopDuration);
                var curve = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.LerpUnclamped(start, peak, curve);
                yield return null;
            }

            target.localScale = start;
        }

        private void BuildPool(SpriteRenderer prefab, int count, Queue<SpriteRenderer> pool)
        {
            if (prefab == null || pool == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab, transform);
                instance.gameObject.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        private SpriteRenderer RentOverlay()
        {
            if (overlayPool.Count == 0)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning("SpellImpactVisualSystem: Overlay pool exhausted; skipping visual.", this);
#endif
                return null;
            }

            var instance = overlayPool.Dequeue();
            activeOverlays.Add(instance);
            instance.gameObject.SetActive(true);
            instance.color = Color.white;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private SpriteRenderer RentGroundPulse()
        {
            if (groundPulsePool.Count == 0)
            {
                return null;
            }

            var instance = groundPulsePool.Dequeue();
            activeGroundPulses.Add(instance);
            instance.gameObject.SetActive(true);
            instance.color = Color.white;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void ConfigureOverlayTransform(Transform target, Vector3 worldPosition)
        {
            target.position = worldPosition;
            target.localScale = Vector3.one;
            target.rotation = Quaternion.identity;
        }

        private void ReturnOverlay(SpriteRenderer overlay)
        {
            if (overlay == null || !activeOverlays.Remove(overlay))
            {
                return;
            }

            overlay.sprite = null;
            overlay.gameObject.SetActive(false);
            overlayPool.Enqueue(overlay);
        }

        private void ReturnGroundPulse(SpriteRenderer pulse)
        {
            if (pulse == null || !activeGroundPulses.Remove(pulse))
            {
                return;
            }

            pulse.sprite = null;
            pulse.gameObject.SetActive(false);
            groundPulsePool.Enqueue(pulse);
        }

        private void OnDestroy()
        {
            foreach (var pair in inFlightLoads)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            foreach (var pair in completedHandles)
            {
                if (pair.Value.IsValid())
                {
                    Addressables.Release(pair.Value);
                }
            }

            inFlightLoads.Clear();
            completedHandles.Clear();
            spriteCache.Clear();
        }
    }
}
