using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TipsConfig", menuName = "SixSeven/Config/TipsConfig")]
public sealed class TipsConfig : ScriptableObject
{
    [Header("Ordered Tips (basic -> advanced)")]
    public List<TipEntry> tips = new List<TipEntry>();

    [Serializable]
    public sealed class TipEntry
    {
        [TextArea]
        public string tip;
    }
}
