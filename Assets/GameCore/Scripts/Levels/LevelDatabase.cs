using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(menuName = "GameCore/Levels/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public int LevelCount => levels?.Count ?? 0;

        public LevelDefinition GetLevel(int index)
        {
            if (levels == null || levels.Count == 0)
            {
                return LevelDefinition.Default;
            }

            var clampedIndex = Mathf.Clamp(index, 0, levels.Count - 1);
            return levels[clampedIndex];
        }
    }

    [System.Serializable]
    public struct LevelDefinition
    {
        // CODEX: LEVEL_LOOP
        public int movesLimit;
        public int targetScore;
        public Vector2Int gridSize;
        // CODEX DIFFICULTY PR7
        public int colorCount;
        // CODEX BOSS PR1
        public bool isBossLevel;

        public static LevelDefinition Default => new LevelDefinition
        {
            // CODEX: LEVEL_LOOP
            movesLimit = 30,
            targetScore = 500,
            gridSize = new Vector2Int(7, 7),
            // CODEX DIFFICULTY PR7
            colorCount = 5,
            // CODEX BOSS PR1
            isBossLevel = false
        };
    }
}
