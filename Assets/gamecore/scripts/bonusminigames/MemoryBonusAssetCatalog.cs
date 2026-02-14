using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(fileName = "MemoryBonusAssetCatalog", menuName = "SixSeven/Memory Bonus Asset Catalog")]
    public sealed class MemoryBonusAssetCatalog : ScriptableObject
    {
        [Serializable]
        private struct FontEntry
        {
            public string Key;
            public Font Font;
        }

        [SerializeField] private List<FontEntry> fonts = new List<FontEntry>();

        public Font GetFont(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var normalized = key.Trim();
            for (var i = 0; i < fonts.Count; i++)
            {
                var entry = fonts[i];
                if (!string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.Font != null)
                {
                    return entry.Font;
                }
            }

            return null;
        }
    }
}
