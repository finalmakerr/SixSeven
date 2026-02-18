using System;

[Serializable]
public class PlayerProfile
{
    public bool hasUnlockedHardcore;
    public bool hasUnlockedIronman;
    public GameMode pendingUnlockMode = GameMode.None;
    public WeeklyModeStats weeklyModeStats = new WeeklyModeStats();
}
