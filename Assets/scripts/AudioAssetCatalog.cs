using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioAssetCatalog", menuName = "SixSeven/Audio Asset Catalog")]
public class AudioAssetCatalog : ScriptableObject
{
    [Serializable]
    public struct AudioEntry
    {
        public string Key;
        public AudioClip Clip;
    }

    [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

    public List<AudioEntry> Entries => entries;

    public AudioClip Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalized = key.Trim();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.Clip != null)
            {
                return entry.Clip;
            }
        }

        return null;
    }


}
