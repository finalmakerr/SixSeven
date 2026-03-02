using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public enum DamageType
    {
        Direct,
        Dot,
        Hazard,
        Boss,
        Spell,
        Unknown
    }

    public class DamageSystem : MonoBehaviour
    {
        public static DamageSystem Instance { get; private set; }

        private readonly Dictionary<int, int> pendingDamage = new Dictionary<int, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // Stage 1: Immediate resolution (NO batching yet)
        public void QueueDamage(int targetId, int amount, DamageType type)
        {
            if (amount <= 0)
                return;

            ApplyImmediateDamage(targetId, amount, type);
        }

        private void ApplyImmediateDamage(int targetId, int amount, DamageType type)
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            if (gm.TryApplyDamageToPlayer(targetId, amount))
                return;

            if (gm.TryApplyDamageToMonster(targetId, amount))
                return;

            gm.TryApplyDamageToBoss(targetId, amount);
        }

        // Future queue support (inactive for now)
        public int GetPendingDamage(int targetId)
        {
            return pendingDamage.TryGetValue(targetId, out var value) ? value : 0;
        }

        public void ClearQueue()
        {
            pendingDamage.Clear();
        }
    }
}
