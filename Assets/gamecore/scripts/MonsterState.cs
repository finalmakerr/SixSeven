using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public struct MonsterState
    {
        public bool IsAngry;
        public bool IsHurt;
        public bool IsCrying;
        public bool IsEnraged;
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
