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
        public bool IsAngry;
        public bool IsEnraged;
        public bool HasCharmResistance;
        public Vector2Int AggressorPosition;
        public int AggressorPieceId;
        public Vector2Int AttackTarget;
        public int TurnsUntilAttack;
        public int TumorShield;
    }
}
