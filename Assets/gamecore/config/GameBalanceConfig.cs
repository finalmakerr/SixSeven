using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "GameCore/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Match Settings")]
    public int MinRunForSpecialBomb = 5;
    public int MinRunForLootRoll = 4;

    [Header("Hazard Settings")]
    public int ToxicGraceStacks = 2;

    [Header("Boss Settings")]
    public int BossAttackDelayTurns = 1;
}
