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
    }
}
