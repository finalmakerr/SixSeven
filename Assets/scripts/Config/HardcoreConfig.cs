using UnityEngine;

/// <summary>
/// Central config controlling all hardcore gameplay modifiers.
/// </summary>
[CreateAssetMenu(fileName = "HardcoreConfig", menuName = "SixSeven/Config/HardcoreConfig")]
public sealed class HardcoreConfig : ScriptableObject
{
    [Header("Level Scaling")]
    // EffectiveLevel = actualLevel + levelOffset
    public int levelOffset = 10;

    [Header("Economy Modifiers")]
    public float shopPriceMultiplier = 1.25f;
    public float sellValueMultiplier = 0.75f;
    public float oneUpPriceMultiplier = 1.5f;
    public int shopRerollLimit = 1; // -1 = unlimited

    [Header("Resource Pressure")]
    public int startingEnergyPenalty = 1;
    public int maxEnergyCapReduction = 1;
    public int maxHpCapReduction = 1;
    public float potionDropMultiplier = 0.8f;
}
