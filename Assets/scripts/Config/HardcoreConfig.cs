using UnityEngine;

/// <summary>
/// Defines pure data modifiers used by hardcore mode systems.
/// </summary>
[CreateAssetMenu(fileName = "HardcoreConfig", menuName = "SixSeven/Config/HardcoreConfig")]
public sealed class HardcoreConfig : ScriptableObject
{
    [Header("Level Scaling")]
    /// <summary>
    /// Amount added to the current level when computing effective level scaling.
    /// </summary>
    public int levelOffset = 10;

    [Header("Economy Modifiers")]
    /// <summary>
    /// Multiplier applied to buy prices in shop contexts.
    /// </summary>
    public float shopPriceMultiplier = 1.25f;

    /// <summary>
    /// Multiplier applied to item sell values.
    /// </summary>
    public float sellValueMultiplier = 0.75f;

    /// <summary>
    /// Multiplier applied to one-up pricing.
    /// </summary>
    public float oneUpPriceMultiplier = 1.5f;

    /// <summary>
    /// Maximum number of shop rerolls allowed per shop session. Use -1 for unlimited.
    /// </summary>
    public int shopRerollLimit = 1; // -1 = unlimited

    [Header("Resource Pressure")]
    /// <summary>
    /// Energy removed from starting value at run start.
    /// </summary>
    public int startingEnergyPenalty = 1;

    /// <summary>
    /// Reduction applied to maximum energy cap.
    /// </summary>
    public int maxEnergyCapReduction = 1;

    /// <summary>
    /// Reduction applied to maximum HP cap.
    /// </summary>
    public int maxHpCapReduction = 1;

    /// <summary>
    /// Multiplier applied to potion drop rates.
    /// </summary>
    public float potionDropMultiplier = 0.8f;
}
