using UnityEngine;

namespace GameCore
{
    public static class DifficultyScaling
    {
        // CODEX DIFFICULTY PR7
        public static LevelDefinition GenerateLevelDefinition(int levelIndex, Vector2Int defaultGridSize)
        {
            var clampedIndex = Mathf.Max(0, levelIndex);
            var movesLimit = Mathf.Max(12, 30 - clampedIndex / 3);
            var targetScore = 500 + (clampedIndex * 150);
            var colorCount = Mathf.Clamp(5 + (clampedIndex / 5), 5, 8);
            var difficultyTier = Mathf.Clamp(1 + (clampedIndex / 4), 1, 10);
            var baseTumorCount = Mathf.Clamp(2 + (clampedIndex / 3), 2, 12);

            return new LevelDefinition
            {
                movesLimit = movesLimit,
                targetScore = targetScore,
                gridSize = defaultGridSize == Vector2Int.zero ? new Vector2Int(7, 7) : defaultGridSize,
                colorCount = colorCount,
                difficultyTier = difficultyTier,
                baseTumorCount = baseTumorCount,
                isBossLevel = false
            };
        }
    }
}
