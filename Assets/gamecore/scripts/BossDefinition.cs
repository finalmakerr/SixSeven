using System.Collections.Generic;
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
        // CODEX BONUS PR6
        public BossPower associatedPower;
        // CODEX COMBAT PR1
        public int tier = 1;
        // CODEX COMBAT PR1
        public MonsterAttackType attackType = MonsterAttackType.Direct;
        // CODEX COMBAT PR1
        public int damage = 0;
        // CODEX COMBAT PR1
        public int range = 2;
        // CODEX BOSS PHASE PR1
        public int maxHP = 100;
        // CODEX BOSS PHASE PR1
        public List<int> phaseThresholdPercentages = new List<int> { 75, 50, 25 };
        // CODEX BOSS PHASE PR1
        public List<BossPower> phaseUnlockedPowers = new List<BossPower>();
        // CODEX BOSS PHASE PR1
        public BossPower enragePower = BossPower.None;
        // CODEX BOSS PHASE PR1
        public float enrageDamageMultiplier = 1.5f;
        // CODEX BOSS PHASE PR1
        public int enrageExtraDamage = 1;
        // CODEX SPECIAL POWERS PR1
        public SpecialPowerBossResistance specialPowerResistance = SpecialPowerBossResistance.None;
        // CODEX BOSS TUMOR SYNERGY PR1
        public BossTumorBehavior tumorBehavior = BossTumorBehavior.None;
        // CODEX BOSS TUMOR SYNERGY PR1
        [Range(0, 3)] public int tumorsPerTurn = 1;
        // CODEX BOSS TUMOR SYNERGY PR1
        [Range(0, 5)] public int tumorUpgradeAttemptsPerTurn = 1;
        // CODEX BOSS TUMOR SYNERGY PR1
        [Range(0, 6)] public int maxBossTumorTier = 4;
        // CODEX BOSS TUMOR SYNERGY PR1
        [Range(0, 5)] public int healPerTumorTier = 1;
        // CODEX BOSS TUMOR SYNERGY PR1
        [Range(0, 8)] public int shieldPerTumorTier = 1;
        // CODEX ENEMY REACTION PR1
        public bool immuneToSwaps = true;
        // CODEX ENEMY REACTION PR1
        public bool preventInstantKillFromPlayerActions = true;
        // CODEX ENEMY REACTION PR1
        public List<SpecialPowerTargetingMode> immuneTargetingModes = new List<SpecialPowerTargetingMode>();
        // CODEX ENEMY REACTION PR1
        public List<SpecialPowerTargetingMode> resistantTargetingModes = new List<SpecialPowerTargetingMode>();
    }
}
