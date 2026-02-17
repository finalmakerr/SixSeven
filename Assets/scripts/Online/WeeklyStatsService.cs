using System;
using System.Globalization;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class WeeklyStatsService
{
    private FirebaseFirestore db;

    public WeeklyStatsService()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    private string GetCurrentWeekKey()
    {
        var now = DateTime.UtcNow;
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var week = calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{now.Year}_W{week}";
    }
}
