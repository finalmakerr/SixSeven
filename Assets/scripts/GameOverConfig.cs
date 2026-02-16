using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameOverConfig", menuName = "SixSeven/Config/GameOverConfig")]
public sealed class GameOverConfig : ScriptableObject
{
    [Header("Death Rules")]
    public bool consumeOneUpOnDeath = true;
    public bool allowHardcoreMode;

    [Header("Resurrection")]
    public string resurrectionVideoPath;
    public bool shopForceOfferOneUp = true;
    public float fadeToBlackDuration = 0.4f;
    public float resurrectionVideoDuration = 2.5f;

    [Header("Weighted Tips")]
    public List<WeightedTip> weightedTips = new List<WeightedTip>();

    [Serializable]
    public sealed class WeightedTip
    {
        [TextArea]
        public string tip;
        [Min(1)] public int weight = 1;
    }
}
