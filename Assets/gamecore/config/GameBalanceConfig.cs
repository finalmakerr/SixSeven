using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "GameCore/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Match Settings")]
    public int MinRunForSpecialBomb = 5;
    public int MinRunForLootRoll = 4;

    [Header("Hazard")]
    public HazardType DefaultHazardType = HazardType.Poison;

    [Header("Hazard Level Thresholds")]
    public int FireHazardLevelThreshold = 20;
    public int IceHazardLevelThreshold = 40;

    [Header("Hazard Settings")]
    public int ToxicGraceStacks = 2;

    [Header("Hazard Pressure")]
    public int HazardGraceTurns = 3;
    public int HazardSpreadInterval = 3;
    public int HazardTileDuration = 3;
    public int GoldenTileDuration = 3;

    [Header("Boss Settings")]
    public int BossAttackDelayTurns = 1;

    [Header("Boss Pickup Scaling")]
    public int BossPickupBaseRadius = 2;
    public int BossPickupLevelThreshold = 60;
    public int BossPickupIncreasedRadius = 3;

    [Header("Difficulty")]
    public int HardcoreLevelOffset = -10;
    public int LevelScalingStep = 10;
}
