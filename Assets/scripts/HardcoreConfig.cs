using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HardcoreConfig", menuName = "SixSeven/Config/HardcoreConfig")]
public sealed class HardcoreConfig : ScriptableObject
{
    [Header("Hardcore Death Rules")]
    [Tooltip("When enabled, the player must permanently remove one option after each death that consumes a 1UP.")]
    public bool removePermanentChoiceOnDeath = true;

    [Min(1)]
    [Tooltip("How many options are shown to the player on each hardcore death choice.")]
    public int optionsPresentedOnDeath = 3;

    [Header("Choice Pool")]
    [Tooltip("Pool used to build the random death choices.")]
    public List<HardcoreRemovalType> choicePool = new List<HardcoreRemovalType>
    {
        HardcoreRemovalType.Spell,
        HardcoreRemovalType.Gear,
        HardcoreRemovalType.ItemSlot
    };
}

[Serializable]
public enum HardcoreRemovalType
{
    Spell,
    Gear,
    ItemSlot
}
