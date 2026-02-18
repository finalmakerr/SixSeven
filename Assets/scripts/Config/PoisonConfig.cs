using UnityEngine;

[CreateAssetMenu(fileName = "PoisonConfig", menuName = "Game/Configs/Poison Config")]
public class PoisonConfig : ScriptableObject
{
    [Header("Initial Delay Before First Rise")]
    public int initialDelayTurns = 7;
    public int hardcoreInitialDelayTurns = 6;

    [Header("Delay Between Row Rises")]
    public int rowRiseDelayTurns = 7;
    public int hardcoreRowRiseDelayTurns = 6;

    [Header("Poison Tier Settings")]
    public int tier2LevelThreshold = 50;
    public int hardcoreTier2Offset = 10;

    [Header("Spread Settings")]
    public int spreadPerTurn = 1;

    public int GetInitialDelay(bool isHardcore)
    {
        return isHardcore ? hardcoreInitialDelayTurns : initialDelayTurns;
    }

    public int GetRowRiseDelay(bool isHardcore)
    {
        return isHardcore ? hardcoreRowRiseDelayTurns : rowRiseDelayTurns;
    }

    public bool IsTier2(int currentLevel, bool isHardcore)
    {
        int threshold = isHardcore
            ? tier2LevelThreshold - hardcoreTier2Offset
            : tier2LevelThreshold;

        return currentLevel >= threshold;
    }
}
