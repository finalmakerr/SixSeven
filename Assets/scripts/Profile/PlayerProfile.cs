using System;

[Serializable]
public class PlayerProfile
{
    public bool hasUnlockedHardcore;
    public bool hasUnlockedIronman;
    public WeeklyModeStats weeklyModeStats = new WeeklyModeStats();
}
