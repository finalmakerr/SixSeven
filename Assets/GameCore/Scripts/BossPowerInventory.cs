using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    // CODEX BOSS PR4
    [Serializable]
    public class BossPowerInventory
    {
        public int MaxSlots => maxSlots;
        public IReadOnlyList<BossPower> Powers => powers;

        [UnityEngine.SerializeField] private int maxSlots = 3;
        [UnityEngine.SerializeField] private List<BossPower> powers = new List<BossPower>();

        public BossPowerInventory()
        {
        }

        public BossPowerInventory(int maxSlots)
        {
            this.maxSlots = Math.Max(1, maxSlots);
        }

        public bool TryAddPower(BossPower power)
        {
            if (powers.Count >= maxSlots)
            {
                return false;
            }

            powers.Add(power);
            return true;
        }

        public string BuildDisplayString()
        {
            if (powers.Count == 0)
            {
                return "Powers: None";
            }

            var builder = new StringBuilder();
            builder.Append("Powers: ");
            for (var i = 0; i < powers.Count; i++)
            {
                builder.Append(powers[i]);
                if (i < powers.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            return builder.ToString();
        }
    }
}
