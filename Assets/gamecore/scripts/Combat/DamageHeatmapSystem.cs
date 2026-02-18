using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class DamageHeatmapSystem : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private readonly Dictionary<Vector2Int, float> rawHeatmap = new Dictionary<Vector2Int, float>();
        private readonly Dictionary<Vector2Int, int> currentHeatmap = new Dictionary<Vector2Int, int>();

        public Dictionary<Vector2Int, int> CurrentHeatmap => currentHeatmap;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }

        public void RecalculateHeatmap()
        {
            rawHeatmap.Clear();
            currentHeatmap.Clear();

            if (gameManager == null)
            {
                return;
            }

            gameManager.PopulateIncomingDamageHeatmap(this);

            foreach (var kvp in rawHeatmap)
            {
                var totalDamage = kvp.Value;
                var tier = totalDamage < 0.5f
                    ? 0
                    : Mathf.Max(1, Mathf.RoundToInt(totalDamage));

                if (tier > 0)
                {
                    currentHeatmap[kvp.Key] = tier;
                }
            }
        }

        public void ClearHeatmap()
        {
            rawHeatmap.Clear();
            currentHeatmap.Clear();
        }

        public void AddPredictedDirectDamage(Vector2Int tile, float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            if (rawHeatmap.TryGetValue(tile, out var existing))
            {
                rawHeatmap[tile] = existing + damage;
                return;
            }

            rawHeatmap[tile] = damage;
        }
    }
}
