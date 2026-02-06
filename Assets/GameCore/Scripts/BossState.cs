using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public struct BossState
    {
        // CODEX BOSS PR1
        public Vector2Int bossPosition;
        // CODEX BOSS PR1
        public bool bossAlive;
        public bool IsEnraged;
        public Vector2Int AttackTarget;
        public int TurnsUntilAttack;
    }
}
