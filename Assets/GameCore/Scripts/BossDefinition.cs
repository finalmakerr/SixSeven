using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public class BossDefinition
    {
        // CODEX BOSS PR1
        public string id;
        // CODEX BOSS PR1
        public string displayName;
        // CODEX BOSS PR1
        public int requiredBombDistance = 2;
        // CODEX BOSS PR1
        public Sprite icon;
        // CODEX BOSS PR1
        public Color color = Color.white;
    }
}
