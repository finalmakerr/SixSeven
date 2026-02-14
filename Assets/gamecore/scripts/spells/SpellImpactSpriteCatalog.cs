using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    [CreateAssetMenu(fileName = "SpellImpactSpriteCatalog", menuName = "SixSeven/Spell Impact Sprite Catalog")]
    public sealed class SpellImpactSpriteCatalog : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public string School;
            public Sprite Sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public Sprite GetSchoolSprite(string school)
        {
            if (string.IsNullOrWhiteSpace(school))
            {
                return null;
            }

            var normalized = school.Trim();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!string.Equals(entry.School, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (entry.Sprite != null)
                {
                    return entry.Sprite;
                }
            }

            return null;
        }
    }
}
