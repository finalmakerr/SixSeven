using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class BossManager : MonoBehaviour
    {
        // CODEX BOSS PR1
        [SerializeField] private List<BossDefinition> bosses = new List<BossDefinition>();

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

            var index = randomSeed > 0
                ? new System.Random(randomSeed).Next(0, bosses.Count)
                : UnityEngine.Random.Range(0, bosses.Count);

            CurrentBoss = bosses[index];

            if (debugMode)
            {
                var bossName = string.IsNullOrEmpty(CurrentBoss.displayName) ? "(unnamed)" : CurrentBoss.displayName;
                Debug.Log($"Boss selected: {CurrentBoss.id} ({bossName})", this);
            }
        }
    }
}
