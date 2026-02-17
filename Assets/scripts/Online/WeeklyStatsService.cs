using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using UnityEngine;

public class WeeklyStatsService
{
    private const string FunctionName = "incrementWeeklyModeWin";

    private FirebaseAuth auth;
    private FirebaseFunctions functions;
    private bool isInitialized;

    public async Task InitializeAsync()
    {
        if (isInitialized)
            return;

        await FirebaseApp.CheckAndFixDependenciesAsync();

        auth = FirebaseAuth.DefaultInstance;
        functions = FirebaseFunctions.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            await auth.SignInAnonymouslyAsync();
            Debug.Log("Firebase anonymous auth succeeded for weekly stats submission.");
        }

        isInitialized = true;
    }

    public async Task SubmitWeeklyWinAsync(GameMode mode, DateTimeOffset runStartUtc, DateTimeOffset runEndUtc, string runId = null)
    {
        try
        {
            await InitializeAsync();

            if (auth.CurrentUser == null)
            {
                Debug.LogError("Weekly win submission rejected: user is not authenticated.");
                return;
            }

            string modeString = mode.ToString().ToLowerInvariant();
            string normalizedRunId = string.IsNullOrWhiteSpace(runId)
                ? Guid.NewGuid().ToString()
                : runId.Trim();

            var payload = new Dictionary<string, object>
            {
                { "mode", modeString },
                { "runId", normalizedRunId },
                { "runStart", runStartUtc.ToUnixTimeSeconds() },
                { "runEnd", runEndUtc.ToUnixTimeSeconds() }
            };

            var callable = functions.GetHttpsCallable(FunctionName);
            var result = await callable.CallAsync(payload);

            Debug.Log($"Weekly win submitted successfully. Response: {result.Data}");
        }
        catch (FunctionsException e)
        {
            Debug.LogError($"Weekly win rejected by Cloud Function ({e.ErrorCode}): {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Weekly win submission failed unexpectedly: {e}");
        }
    }

    public async Task SubmitWeeklyWinAsync(GameMode mode, long runStartUnix, long runEndUnix, string runId = null)
    {
        await SubmitWeeklyWinAsync(
            mode,
            DateTimeOffset.FromUnixTimeSeconds(runStartUnix),
            DateTimeOffset.FromUnixTimeSeconds(runEndUnix),
            runId);
    }
}
