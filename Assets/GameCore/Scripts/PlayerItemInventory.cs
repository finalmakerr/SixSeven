using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    [Serializable]
    public class PlayerItemInventory
    {
        public int MaxSlots => maxSlots;
        public IReadOnlyList<PlayerItemType> Items => items;
        public int Count => items.Count;

        [UnityEngine.SerializeField] private int maxSlots = 3;
        [UnityEngine.SerializeField] private List<PlayerItemType> items = new List<PlayerItemType>();

        public PlayerItemInventory()
        {
        }

        public PlayerItemInventory(int maxSlots)
        {
            this.maxSlots = Math.Max(1, maxSlots);
        }

        public bool TryAddItem(PlayerItemType item)
        {
            if (items.Count >= maxSlots)
            {
                return false;
            }

            items.Add(item);
            return true;
        }

        public bool TryConsumeItem(PlayerItemType item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != item)
                {
                    continue;
                }

                items.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool HasItem(PlayerItemType item)
        {
            return items.Contains(item);
        }

        public int CountOf(PlayerItemType item)
        {
            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] == item)
                {
                    count += 1;
                }
            }

            return count;
        }

        public void Clear()
        {
            items.Clear();
        }

        public string BuildDisplayString()
        {
            if (items.Count == 0)
            {
                return "Inventory: Empty";
            }

            var builder = new StringBuilder();
            builder.Append("Inventory: ");
            for (var i = 0; i < items.Count; i++)
            {
                builder.Append(items[i]);
                if (i < items.Count - 1)
                {
                    builder.Append(", ");
                }
            }

            return builder.ToString();
        }
    }
}
