using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    /// <summary>
    /// Authoritative pooled spell-impact feedback.
    /// Uses one sprite per school from a SpellImpactSpriteCatalog loaded by SceneAssetLoader.
    /// </summary>
    public sealed class SpellImpactVisualSystem : MonoBehaviour
    {
        public const int TierNormal = 1;
        public const int TierImportant = 2;

        private const string FallbackSchool = "_default";

        [Header("Render")]
        [SerializeField] private int normalSortingOrder = 80;
        [SerializeField] private int importantSortingOrder = 95;
        [SerializeField] private Color overlayColor = Color.white;
        [SerializeField] private Color groundPulseColor = new Color(0f, 0f, 0f, 0.4f);

        [Header("Durations")]
        [SerializeField] private float normalDuration = 0.12f;
        [SerializeField] private float importantDuration = 0.18f;
        [SerializeField] private float normalFreezeSeconds = 0.03f;
        [SerializeField] private float importantFreezeSeconds = 0.06f;
        [SerializeField] private float randomTimingVariance = 0.01f;

        [Header("Randomization")]
        [SerializeField] private float randomRotationRange = 12f;
        [SerializeField] private float randomScalePercent = 0.05f;

        [Header("Tier 2 Extras")]
        [SerializeField] private bool enableTinyCameraShake = true;
        [SerializeField] private Transform cameraShakeTarget;
        [SerializeField] private float cameraShakeDuration = 0.06f;
        [SerializeField] private float cameraShakeMagnitude = 0.05f;

        [Header("Audio")]
        [SerializeField] private AudioSource defaultAudioSource;
        [SerializeField] private float tier2VolumeMultiplier = 1.2f;

        [Header("Assets")]
        [SerializeField] private SceneAssetLoader sceneAssetLoader;

        [Header("Pool")]
        [SerializeField] private int prewarmOverlayCount = 16;
        [SerializeField] private int prewarmPulseCount = 8;

        public static SpellImpactVisualSystem Instance { get; private set; }
        public event Action<bool> OnVisualFreezeChanged;

        private readonly Queue<PooledVisual> overlayPool = new Queue<PooledVisual>();
        private readonly Queue<PooledVisual> pulsePool = new Queue<PooledVisual>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private SpellImpactSpriteCatalog spriteCatalog;

        private uint randomState = 0x12345678u;
        private float importantPriorityUntil;
        private int freezeRefCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PrewarmPool(overlayPool, prewarmOverlayCount, "SpellOverlay");
            PrewarmPool(pulsePool, prewarmPulseCount, "SpellGroundPulse");
        }

        public void PlaySpellImpact(SpellImpactRequest request)
        {
            var tier = request.Tier >= TierImportant ? TierImportant : TierNormal;
            var important = tier == TierImportant;
            var sprite = LoadSchoolSprite(request.School);
            if (sprite == null)
            {
                return;
            }

            var duration = important ? importantDuration : normalDuration;
            duration = Mathf.Max(0.05f, duration + RandomRange(-randomTimingVariance, randomTimingVariance));

            var color = overlayColor;
            if (!important && Time.unscaledTime < importantPriorityUntil)
            {
                duration *= 0.75f;
                color.a *= 0.65f;
            }

            var overlay = GetOrCreate(overlayPool, "SpellOverlay");
            overlay.SpriteRenderer.sprite = sprite;
            overlay.SpriteRenderer.sortingOrder = important ? importantSortingOrder : normalSortingOrder;

            var startScale = 0.6f * RandomScaleMultiplier();
            var midScale = (important ? 1.25f : 1.05f) * RandomScaleMultiplier();
            var endScale = 1f * RandomScaleMultiplier();
            var rotation = RandomRange(-randomRotationRange, randomRotationRange);

            overlay.gameObject.SetActive(true);
            overlay.transform.position = request.TileCenter;
            overlay.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            overlay.transform.localScale = Vector3.one * startScale;
            overlay.SpriteRenderer.color = new Color(color.r, color.g, color.b, 0f);

            if (important)
            {
                importantPriorityUntil = Time.unscaledTime + duration;
                StartCoroutine(PlayGroundPulse(request.TileCenter, sprite));
                if (enableTinyCameraShake && cameraShakeTarget != null)
                {
                    StartCoroutine(DoCameraShake());
                }
            }

            StartCoroutine(AnimateImpactRoutine(overlay, request, important, duration, startScale, midScale, endScale, color));
        }

        public void ApplySpellButtonFeedback(Image target, string school, int tier)
        {
            if (target == null)
            {
                return;
            }

            var sprite = LoadSchoolSprite(school);
            if (sprite == null)
            {
                return;
            }

            target.sprite = sprite;
            StartCoroutine(ButtonFeedbackRoutine(target, tier >= TierImportant));
        }

        private IEnumerator AnimateImpactRoutine(
            PooledVisual overlay,
            SpellImpactRequest request,
            bool important,
            float duration,
            float startScale,
            float midScale,
            float endScale,
            Color color)
        {
            var freezeTime = important ? importantFreezeSeconds : normalFreezeSeconds;
            var elapsed = 0f;
            var peakSent = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                if (!peakSent && t >= 0.5f)
                {
                    peakSent = true;
                    PlayImpactAudio(request, important);
                    StartCoroutine(FreezeVisualsRoutine(freezeTime));
                }

                var halfT = t < 0.5f ? t / 0.5f : (t - 0.5f) / 0.5f;
                var scale = t < 0.5f
                    ? Mathf.Lerp(startScale, midScale, halfT)
                    : Mathf.Lerp(midScale, endScale, halfT);
                var alpha = t < 0.5f
                    ? Mathf.Lerp(0f, 1f, halfT)
                    : Mathf.Lerp(1f, 0f, halfT);

                overlay.transform.localScale = Vector3.one * scale;
                overlay.SpriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            overlay.StopAndReset();
            overlayPool.Enqueue(overlay);
        }

        private IEnumerator PlayGroundPulse(Vector3 center, Sprite sprite)
        {
            var pulse = GetOrCreate(pulsePool, "SpellGroundPulse");
            pulse.SpriteRenderer.sprite = sprite;
            pulse.SpriteRenderer.sortingOrder = importantSortingOrder - 1;

            const float duration = 0.15f;
            const float startScale = 0.5f;
            const float endScale = 1.4f;

            pulse.gameObject.SetActive(true);
            pulse.transform.position = center;
            pulse.transform.rotation = Quaternion.identity;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

                var color = groundPulseColor;
                color.a = Mathf.Lerp(0.4f, 0f, t);
                pulse.SpriteRenderer.color = color;
                yield return null;
            }

            pulse.StopAndReset();
            pulsePool.Enqueue(pulse);
        }

        private IEnumerator FreezeVisualsRoutine(float seconds)
        {
            freezeRefCount++;
            if (freezeRefCount == 1)
            {
                OnVisualFreezeChanged?.Invoke(true);
            }

            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            freezeRefCount = Mathf.Max(0, freezeRefCount - 1);
            if (freezeRefCount == 0)
            {
                OnVisualFreezeChanged?.Invoke(false);
            }
        }

        private void PlayImpactAudio(SpellImpactRequest request, bool important)
        {
            if (request.ImpactClip == null)
            {
                return;
            }

            var source = request.AudioSourceOverride != null ? request.AudioSourceOverride : defaultAudioSource;
            if (source == null)
            {
                return;
            }

            var volume = Mathf.Clamp01(request.Volume * (important ? tier2VolumeMultiplier : 1f));
            source.PlayOneShot(request.ImpactClip, volume);
        }

        private IEnumerator DoCameraShake()
        {
            var basePos = cameraShakeTarget.localPosition;
            var elapsed = 0f;

            while (elapsed < cameraShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var x = RandomRange(-cameraShakeMagnitude, cameraShakeMagnitude);
                var y = RandomRange(-cameraShakeMagnitude, cameraShakeMagnitude);
                cameraShakeTarget.localPosition = basePos + new Vector3(x, y, 0f);
                yield return null;
            }

            cameraShakeTarget.localPosition = basePos;
        }

        private Sprite LoadSchoolSprite(string school)
        {
            var key = NormalizeSchool(school);
            if (spriteCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            EnsureSpriteCatalogLoaded();
            Sprite sprite = null;

            if (spriteCatalog != null)
            {
                sprite = spriteCatalog.GetSchoolSprite(key);
                if (sprite == null)
                {
                    sprite = spriteCatalog.GetSchoolSprite(FallbackSchool);
                }
            }

            spriteCache[key] = sprite;
            return sprite;
        }

        private void EnsureSpriteCatalogLoaded()
        {
            if (spriteCatalog != null)
            {
                return;
            }

            if (sceneAssetLoader == null)
            {
                return;
            }

            spriteCatalog = sceneAssetLoader.GetLoadedAsset<SpellImpactSpriteCatalog>();
        }

        private static string NormalizeSchool(string school)
        {
            return string.IsNullOrWhiteSpace(school)
                ? FallbackSchool
                : school.Trim().ToLowerInvariant();
        }

        private void PrewarmPool(Queue<PooledVisual> pool, int count, string objectName)
        {
            for (var i = 0; i < count; i++)
            {
                var visual = CreateVisual(objectName);
                visual.gameObject.SetActive(false);
                pool.Enqueue(visual);
            }
        }

        private PooledVisual GetOrCreate(Queue<PooledVisual> pool, string objectName)
        {
            return pool.Count > 0 ? pool.Dequeue() : CreateVisual(objectName);
        }

        private PooledVisual CreateVisual(string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            return new PooledVisual(go, renderer);
        }

        private float RandomScaleMultiplier()
        {
            return 1f + RandomRange(-randomScalePercent, randomScalePercent);
        }

        private float RandomRange(float min, float max)
        {
            randomState = randomState * 1664525u + 1013904223u;
            var unit = (randomState & 0x00FFFFFFu) / 16777215f;
            return Mathf.Lerp(min, max, unit);
        }

        private sealed class PooledVisual
        {
            public readonly GameObject gameObject;
            public readonly Transform transform;
            public readonly SpriteRenderer SpriteRenderer;

            public PooledVisual(GameObject gameObject, SpriteRenderer spriteRenderer)
            {
                this.gameObject = gameObject;
                transform = gameObject.transform;
                SpriteRenderer = spriteRenderer;
            }

            public void StopAndReset()
            {
                SpriteRenderer.sprite = null;
                SpriteRenderer.color = Color.clear;
                transform.localScale = Vector3.one;
                gameObject.SetActive(false);
            }
        }
    }

    [Serializable]
    public struct SpellImpactRequest
    {
        public string School;
        public int Tier;
        public Vector3 TileCenter;
        public AudioClip ImpactClip;
        public AudioSource AudioSourceOverride;
        public float Volume;

        public SpellImpactRequest(string school, int tier, Vector3 tileCenter, AudioClip impactClip = null, AudioSource audioSourceOverride = null, float volume = 1f)
        {
            School = school;
            Tier = tier;
            TileCenter = tileCenter;
            ImpactClip = impactClip;
            AudioSourceOverride = audioSourceOverride;
            Volume = volume;
        }
    }
}
