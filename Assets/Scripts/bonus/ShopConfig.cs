using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Configs/ShopConfig")]
public class ShopConfig : ScriptableObject
{
    [Header("1UP Spawn Settings")]
    public float baseOneUpSpawnChance = 100f;
    public float oneUpSpawnPenaltyPerLife = 20f;
    public float minOneUpSpawnChance = 40f;

    public float GetOneUpSpawnChancePercent(int extraLives)
    {
        float chance = baseOneUpSpawnChance - (oneUpSpawnPenaltyPerLife * extraLives);
        return Mathf.Max(minOneUpSpawnChance, chance);
    }
}
