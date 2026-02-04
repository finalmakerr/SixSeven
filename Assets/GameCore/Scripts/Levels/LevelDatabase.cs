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
        public int moves;
        public int targetScore;
        public Vector2Int gridSize;

        public static LevelDefinition Default => new LevelDefinition
        {
            moves = 30,
            targetScore = 500,
            gridSize = new Vector2Int(7, 7)
        };
    }
}
