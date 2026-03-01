using System;
using UnityEngine;

namespace SixSeven.Systems
{
    [Serializable]
    public sealed class BossVitalsSystem
    {
        [SerializeField] private int maxHp;
        [SerializeField] private int currentHp;
        [SerializeField] private int shieldUnits;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public int ShieldUnits => shieldUnits;
        public bool IsAlive => currentHp > 0;

        public void Initialize(int initialMaxHp)
        {
            maxHp = Mathf.Max(1, initialMaxHp);
            currentHp = maxHp;
            shieldUnits = 0;
        }

        public DamageResolution ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return new DamageResolution(false, 0, 0);
            }

            var remainingDamage = amount;
            var shieldAbsorbed = Mathf.Min(shieldUnits, remainingDamage);
            shieldUnits -= shieldAbsorbed;
            remainingDamage -= shieldAbsorbed;

            var hpLost = Mathf.Min(currentHp, remainingDamage);
            currentHp -= hpLost;

            return new DamageResolution(currentHp <= 0, hpLost, shieldAbsorbed);
        }

        public int ApplyHeal(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var previousHp = currentHp;
            currentHp = Mathf.Clamp(currentHp + amount, 0, maxHp);
            return currentHp - previousHp;
        }

        public int ApplyShieldDamage(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var absorbed = Mathf.Min(shieldUnits, amount);
            shieldUnits -= absorbed;
            return absorbed;
        }

        public int AddShield(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            shieldUnits += amount;
            return amount;
        }

        public void SetCurrentHp(int amount)
        {
            currentHp = Mathf.Clamp(amount, 0, maxHp);
        }

        public readonly struct DamageResolution
        {
            public readonly bool IsFatal;
            public readonly int HpLost;
            public readonly int ShieldAbsorbed;

            public DamageResolution(bool isFatal, int hpLost, int shieldAbsorbed)
            {
                IsFatal = isFatal;
                HpLost = hpLost;
                ShieldAbsorbed = shieldAbsorbed;
            }
        }
    }
}
