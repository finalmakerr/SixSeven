using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class BossManager : MonoBehaviour
    {
        // CODEX BOSS PR1
        [SerializeField] private List<BossDefinition> bosses = new List<BossDefinition>();
        // CODEX BONUS PR6
        private readonly HashSet<BossDefinition> removedBossesThisRun = new HashSet<BossDefinition>();

        // CODEX BOSS PR1
        public BossDefinition CurrentBoss { get; private set; }

        // CODEX BOSS PR1
        public void SelectBossForRun(int randomSeed, bool debugMode)
        {
            if (CurrentBoss != null)
            {
                return;
            }

            if (bosses == null || bosses.Count == 0)
            {
                return;
            }

            var availableBosses = new List<BossDefinition>();
            for (var i = 0; i < bosses.Count; i++)
            {
                var boss = bosses[i];
                if (boss == null || removedBossesThisRun.Contains(boss))
                {
                    continue;
                }

                availableBosses.Add(boss);
            }

            if (availableBosses.Count == 0)
            {
                return;
            }

            var index = randomSeed > 0
                ? new System.Random(randomSeed).Next(0, availableBosses.Count)
                : UnityEngine.Random.Range(0, availableBosses.Count);

            CurrentBoss = availableBosses[index];

            if (debugMode)
            {
                var bossName = string.IsNullOrEmpty(CurrentBoss.displayName) ? "(unnamed)" : CurrentBoss.displayName;
                Debug.Log($"Boss selected: {CurrentBoss.id} ({bossName})", this);
            }
        }

        // CODEX BONUS PR6
        public void RemoveBossFromRunPool(BossPower power)
        {
            if (bosses == null || bosses.Count == 0)
            {
                return;
            }

            for (var i = 0; i < bosses.Count; i++)
            {
                var boss = bosses[i];
                if (boss == null || boss.associatedPower != power)
                {
                    continue;
                }

                removedBossesThisRun.Add(boss);
                return;
            }
        }

        // CODEX BONUS PR6
        public void ResetRunPool()
        {
            removedBossesThisRun.Clear();
        }
    }
}
