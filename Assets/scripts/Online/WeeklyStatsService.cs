using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Functions;
using Firebase.Extensions;
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

    public async Task SubmitWeeklyWinAsync(GameMode mode)
    {
        if (!FirebaseInitializer.IsReady)
            return;

        var functions = FirebaseFunctions.DefaultInstance;

        string modeString = mode.ToString().ToLower();

        var data = new Dictionary<string, object>
        {
            { "mode", modeString }
        };

        try
        {
            await functions
                .GetHttpsCallable("incrementWeeklyModeWin")
                .CallAsync(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Weekly win submission failed: {e}");
        }
    }
}
