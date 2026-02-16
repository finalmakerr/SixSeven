using System;

[Serializable]
public class WeeklyModeStats
{
    public int normalCompleted;
    public int hardcoreCompleted;
    public int ironmanCompleted;
    // Unity JsonUtility cannot serialize DateTime reliably; keep as ISO-8601 string.
    public string weekStartUtc;
}
