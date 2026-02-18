using UnityEngine;

namespace GameCore
{
    [System.Serializable]
    public class MonsterAggroConfig
    {
        public int defaultMonsterHP = 3;
        public int enrageDuration = 1; // future-proof if we extend enrage window
        public int turnsBeforeAttack = 2;
        public int confusedDuration = 2;
        public int tiredDuration = 1;
        public int sleepDuration = 1;
        public bool adjacencyRequiresMatchForecast = true;
        public bool damageTriggerAllowed = true;
        public bool telegraphOnlyOnEnrage = true;
        public bool requireHpSurvivalCheck = true;
    }
}
