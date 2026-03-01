using UnityEngine;
using SixSeven.Core;

namespace GameCore
{
    [System.Serializable]
    public struct MonsterState
    {
        public bool IsIdle;
        public EnrageState EnrageState;
        public StatusContainer Statuses;

        public Vector2Int TargetTile;

        public int TurnsUntilAttack;
        public Vector2Int CurrentTile;
        public int CurrentHP;
    }
}
