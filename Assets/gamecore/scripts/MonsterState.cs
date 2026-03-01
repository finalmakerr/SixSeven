using UnityEngine;
using SixSeven.Core;

namespace GameCore
{
    [System.Serializable]
    public struct MonsterState
    {
        public bool IsIdle;
        public EnrageState EnrageState;
        public bool IsHurt;
        public bool IsTired;
        public bool IsSleeping;
        public bool IsConfused;

        public Vector2Int TargetTile;

        public int TurnsUntilAttack;
        public int StateTurnsRemaining;
        public Vector2Int CurrentTile;
        public int CurrentHP;
    }
}
