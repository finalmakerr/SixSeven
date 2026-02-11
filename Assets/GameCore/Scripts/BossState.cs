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
        public int MaxHP;
        public int CurrentHP;
        public int CurrentPhaseIndex;
        public bool IsPermanentlyEnraged;
        public bool IsEnraged;
        public Vector2Int AttackTarget;
        public int TurnsUntilAttack;
    }
}
