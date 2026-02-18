using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public class MonsterAngerConfig
    {
        public bool allowDamageTrigger = true;
        public bool angerAdjacencyRequired = true;
        public int minorDamageThreshold = 1;
        public int maxAngryPerTurn = 1;
        public int aggressionScalingByLevel = 6;
        public int defaultMonsterHP = 3;
        public int turnsBeforeAttack = 2; // 1 = enrage tick, 1 = attack tick
        public bool requireHpSurvivalCheck = true;
    }
}
