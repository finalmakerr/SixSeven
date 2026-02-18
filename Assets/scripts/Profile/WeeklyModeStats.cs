using System;

[Serializable]
public class WeeklyModeStats
{
    public int normalCompleted;
    public int hardcoreCompleted;
    public int ironmanCompleted;

    public DateTime weekStartUtc;
}
