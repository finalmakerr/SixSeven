using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TipsConfig", menuName = "SixSeven/Config/TipsConfig")]
public sealed class TipsConfig : ScriptableObject
{
    [Header("Ordered Tips (basic -> advanced)")]
    public List<WeightedTip> orderedTips = new List<WeightedTip>();

    [Serializable]
    public sealed class WeightedTip
    {
        [TextArea]
        public string tip;
        [Min(1)] public int weight = 1;
    }
}
