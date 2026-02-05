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

            return new LevelDefinition
            {
                movesLimit = movesLimit,
                targetScore = targetScore,
                gridSize = defaultGridSize == Vector2Int.zero ? new Vector2Int(7, 7) : defaultGridSize,
                colorCount = colorCount,
                isBossLevel = false
            };
        }
    }
}
