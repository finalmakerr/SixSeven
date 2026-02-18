using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class DamageHeatmapOverlay : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private GameObject damageHeatOverlayPrefab;
        [SerializeField] private float basePulseSpeed = 2.25f;
        [SerializeField] private float pulseSpeedPerTier = 0.55f;
        [SerializeField] private float basePulseStrength = 0.07f;
        [SerializeField] private float pulseStrengthPerTier = 0.05f;

        private readonly Dictionary<Vector2Int, OverlayState> activeOverlays = new Dictionary<Vector2Int, OverlayState>();
        private readonly List<Vector2Int> removalBuffer = new List<Vector2Int>();

        private bool isFadedMode;
        private bool pulseAnimationEnabled = true;

        private sealed class OverlayState
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public int Tier;
            public int TargetTier;
            public Color Color;
            public Color TargetColor;
            public float PulseStrength;
            public float PulseSpeed;
            public Vector3 BaseScale;
        }

        private void Awake()
        {
            if (board == null)
            {
                board = FindObjectOfType<Board>();
            }
        }

        private void OnDisable()
        {
            ClearAll();
        }


        public void SetFadedMode(bool faded)
        {
            isFadedMode = faded;
        }

        public void SetPulseAnimationEnabled(bool enabled)
        {
            pulseAnimationEnabled = enabled;
        }

        private void Update()
        {
            foreach (var overlay in activeOverlays.Values)
            {
                if (overlay == null || overlay.GameObject == null)
                {
                    continue;
                }

                var targetColor = overlay.TargetColor;
                var targetAlpha = isFadedMode ? 0.2f : targetColor.a;
                targetColor.a = targetAlpha;

                overlay.Color = Color.Lerp(overlay.Color, targetColor, Time.deltaTime * 12f);
                overlay.PulseStrength = Mathf.Lerp(overlay.PulseStrength, GetPulseStrengthForTier(overlay.TargetTier), Time.deltaTime * 8f);
                overlay.PulseSpeed = Mathf.Lerp(overlay.PulseSpeed, GetPulseSpeedForTier(overlay.TargetTier), Time.deltaTime * 8f);
                overlay.Tier = overlay.TargetTier;

                if (overlay.Renderer != null)
                {
                    overlay.Renderer.color = overlay.Color;
                }

                var pulseEnabled = pulseAnimationEnabled && !isFadedMode;
                if (pulseEnabled)
                {
                    var pulse = (Mathf.Sin(Time.time * overlay.PulseSpeed) + 1f) * 0.5f;
                    overlay.GameObject.transform.localScale = overlay.BaseScale * (1f + pulse * overlay.PulseStrength);
                }
                else
                {
                    overlay.GameObject.transform.localScale = overlay.BaseScale;
                }
            }
        }

        public void Render(Dictionary<Vector2Int, int> heatmap)
        {
            if (board == null || damageHeatOverlayPrefab == null)
            {
                ClearAll();
                return;
            }

            removalBuffer.Clear();
            foreach (var kvp in activeOverlays)
            {
                if (heatmap == null || !heatmap.TryGetValue(kvp.Key, out var tier) || tier <= 0)
                {
                    removalBuffer.Add(kvp.Key);
                    continue;
                }

                UpdateOverlay(kvp.Key, tier, kvp.Value);
            }

            for (var i = 0; i < removalBuffer.Count; i++)
            {
                RemoveOverlay(removalBuffer[i]);
            }

            if (heatmap == null)
            {
                return;
            }

            foreach (var kvp in heatmap)
            {
                if (kvp.Value <= 0 || activeOverlays.ContainsKey(kvp.Key))
                {
                    continue;
                }

                CreateOverlay(kvp.Key, kvp.Value);
            }
        }

        private void CreateOverlay(Vector2Int tile, int tier)
        {
            var worldPosition = board.GridToWorld(tile.x, tile.y);
            var instance = Instantiate(damageHeatOverlayPrefab, worldPosition, Quaternion.identity, transform);
            instance.name = $"DamageHeatOverlay_{tile.x}_{tile.y}";

            var renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = instance.GetComponentInChildren<SpriteRenderer>();
            }

            var targetColor = GetColorForTier(tier);
            var state = new OverlayState
            {
                GameObject = instance,
                Renderer = renderer,
                Tier = tier,
                TargetTier = tier,
                Color = targetColor,
                TargetColor = targetColor,
                PulseStrength = GetPulseStrengthForTier(tier),
                PulseSpeed = GetPulseSpeedForTier(tier),
                BaseScale = instance.transform.localScale
            };

            if (renderer != null)
            {
                renderer.color = targetColor;
            }

            activeOverlays[tile] = state;
        }

        private void UpdateOverlay(Vector2Int tile, int tier, OverlayState state)
        {
            if (state == null || state.GameObject == null)
            {
                RemoveOverlay(tile);
                CreateOverlay(tile, tier);
                return;
            }

            state.GameObject.transform.position = board.GridToWorld(tile.x, tile.y);
            state.TargetTier = tier;
            state.TargetColor = GetColorForTier(tier);
        }

        private void RemoveOverlay(Vector2Int tile)
        {
            if (!activeOverlays.TryGetValue(tile, out var state))
            {
                return;
            }

            if (state?.GameObject != null)
            {
                Destroy(state.GameObject);
            }

            activeOverlays.Remove(tile);
        }

        private void ClearAll()
        {
            foreach (var state in activeOverlays.Values)
            {
                if (state?.GameObject != null)
                {
                    Destroy(state.GameObject);
                }
            }

            activeOverlays.Clear();
        }

        private static Color GetColorForTier(int tier)
        {
            if (tier <= 1)
            {
                return Color.yellow;
            }

            if (tier == 2)
            {
                return new Color(1f, 0.55f, 0f, 0.92f);
            }

            if (tier == 3)
            {
                return new Color(1f, 0.12f, 0.12f, 0.95f);
            }

            return new Color(0.48f, 0f, 0f, 0.98f);
        }

        private float GetPulseStrengthForTier(int tier)
        {
            return basePulseStrength + Mathf.Max(0, tier - 1) * pulseStrengthPerTier;
        }

        private float GetPulseSpeedForTier(int tier)
        {
            return basePulseSpeed + Mathf.Max(0, tier - 1) * pulseSpeedPerTier;
        }
    }
}
