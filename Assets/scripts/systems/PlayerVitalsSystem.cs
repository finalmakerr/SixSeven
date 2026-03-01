using System;
using UnityEngine;

namespace SixSeven.Systems
{
    /// <summary>
    /// Core HP and Energy model using half-heart precision:
    /// 1 unit = 1 HP = half heart.
    /// </summary>
    [Serializable]
    public sealed class PlayerVitalsSystem
    {
        public const int HpPerHeart = 2;
        public const int ShieldHpPerHeart = 1;
        public const int MaxHearts = 7;

        [SerializeField] private int unlockedHearts = 3;
        [SerializeField] private int hpUnits = 6;
        [SerializeField] private int shieldUnits;
        [SerializeField] private int energyUnlockedHearts = 3;
        [SerializeField] private int energyUnits = 6;

        public int UnlockedHearts => unlockedHearts;
        public int MaxBaseHp => unlockedHearts * HpPerHeart;
        public int MaxShieldHp => unlockedHearts * ShieldHpPerHeart;
        public int MaxTotalHp => MaxBaseHp + MaxShieldHp;
        public int CurrentHp => hpUnits;
        public int CurrentShield => shieldUnits;
        public int CurrentTotalHp => hpUnits + shieldUnits;

        public int EnergyUnlockedHearts => energyUnlockedHearts;
        public int MaxEnergy => energyUnlockedHearts * HpPerHeart;
        public int CurrentEnergy => energyUnits;

        public void SetUnlockedHearts(int hearts)
        {
            unlockedHearts = Mathf.Clamp(hearts, 1, MaxHearts);
            hpUnits = Mathf.Clamp(hpUnits, 0, MaxBaseHp);
            shieldUnits = Mathf.Clamp(shieldUnits, 0, MaxShieldHp);
        }

        public void SetEnergyUnlockedHearts(int hearts)
        {
            energyUnlockedHearts = Mathf.Clamp(hearts, 1, MaxHearts);
            energyUnits = Mathf.Clamp(energyUnits, 0, MaxEnergy);
        }

        public void RefillHpToBaseMaximum()
        {
            hpUnits = MaxBaseHp;
        }

        public void SetCurrentHp(int amount)
        {
            hpUnits = Mathf.Clamp(amount, 0, MaxBaseHp);
        }

        public void RefillEnergyToMaximum()
        {
            energyUnits = MaxEnergy;
        }

        public int RestoreHp(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var previous = hpUnits;
            hpUnits = Mathf.Clamp(hpUnits + amount, 0, MaxBaseHp);
            return hpUnits - previous;
        }

        public int ApplyHeal(int amount)
        {
            return RestoreHp(amount);
        }

        public int AddShield(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var previous = shieldUnits;
            shieldUnits = Mathf.Clamp(shieldUnits + amount, 0, MaxShieldHp);
            return shieldUnits - previous;
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

        public int GainEnergy(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var previous = energyUnits;
            energyUnits = Mathf.Clamp(energyUnits + amount, 0, MaxEnergy);
            return energyUnits - previous;
        }

        public bool CanSpendEnergy(int amount)
        {
            return amount >= 0 && energyUnits >= amount;
        }

        public bool TrySpendEnergy(int amount)
        {
            if (!CanSpendEnergy(amount))
            {
                return false;
            }

            energyUnits -= amount;
            return true;
        }

        /// <summary>
        /// Damage order:
        /// 1) Shield HP
        /// 2) Base HP
        /// </summary>
        public DamageResolution ApplyDamage(int amount)
        {
            if (amount <= 0)
            {
                return new DamageResolution(0, 0, false);
            }

            var remainingDamage = amount;
            var shieldLost = Mathf.Min(shieldUnits, remainingDamage);
            shieldUnits -= shieldLost;
            remainingDamage -= shieldLost;

            var hpLost = Mathf.Min(hpUnits, remainingDamage);
            hpUnits -= hpLost;

            return new DamageResolution(shieldLost, hpLost, hpUnits <= 0);
        }

        public readonly struct DamageResolution
        {
            public readonly int ShieldLost;
            public readonly int HpLost;
            public readonly bool IsFatal;

            public DamageResolution(int shieldLost, int hpLost, bool isFatal)
            {
                ShieldLost = shieldLost;
                HpLost = hpLost;
                IsFatal = isFatal;
            }
        }
    }
}
