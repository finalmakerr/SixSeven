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
    private const int MinimumRunDurationSeconds = 300;

    private FirebaseAuth auth;
    private FirebaseFunctions functions;
    private bool isInitialized;

    private string activeRunId;
    private long activeRunStartUnix;
    private long activeRunEndUnix;
    private readonly HashSet<string> submittedRunIds = new HashSet<string>();

    public async Task Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase initialization failed. Dependency status: {dependencyStatus}");
            return;
        }

        auth = FirebaseAuth.DefaultInstance;
        functions = FirebaseFunctions.DefaultInstance;

        if (auth == null || functions == null)
        {
            Debug.LogError("Firebase initialization failed. Auth or Functions instance was null.");
            return;
        }

        if (auth.CurrentUser == null)
        {
            try
            {
                await auth.SignInAnonymouslyAsync();
                Debug.Log("Firebase anonymous auth succeeded for weekly win submission.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Firebase anonymous auth failed: {exception}");
                return;
            }
        }

        isInitialized = true;
        Debug.Log("WeeklyStatsService initialized.");
    }

    public void StartRun()
    {
        activeRunId = Guid.NewGuid().ToString("D");
        activeRunStartUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        activeRunEndUnix = 0;

        Debug.Log($"Run started. runId={activeRunId}, runStart={activeRunStartUnix}");
    }

    public void EndRun()
    {
        if (string.IsNullOrWhiteSpace(activeRunId))
        {
            Debug.LogError("EndRun failed: no active run was started.");
            return;
        }

        activeRunEndUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Debug.Log($"Run ended. runId={activeRunId}, runEnd={activeRunEndUnix}");
    }

    public async Task<bool> SubmitWinAsync(GameMode mode)
    {
        try
        {
            await Initialize();

            if (!isInitialized)
            {
                Debug.LogError("SubmitWinAsync failed: service is not initialized.");
                return false;
            }

            if (auth == null || auth.CurrentUser == null)
            {
                Debug.LogError("SubmitWinAsync failed: user is not authenticated.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(activeRunId))
            {
                Debug.LogError("SubmitWinAsync failed: runId is missing. Call StartRun() first.");
                return false;
            }

            if (activeRunStartUnix <= 0)
            {
                Debug.LogError("SubmitWinAsync failed: runStart is invalid. Call StartRun() first.");
                return false;
            }

            if (activeRunEndUnix <= activeRunStartUnix)
            {
                Debug.LogError("SubmitWinAsync failed: runEnd is invalid. Call EndRun() before submit.");
                return false;
            }

            long runDuration = activeRunEndUnix - activeRunStartUnix;
            if (runDuration < MinimumRunDurationSeconds)
            {
                Debug.LogWarning($"SubmitWinAsync rejected locally: run duration {runDuration}s is less than {MinimumRunDurationSeconds}s.");
                return false;
            }

            if (submittedRunIds.Contains(activeRunId))
            {
                Debug.LogWarning($"SubmitWinAsync rejected locally: duplicate runId detected ({activeRunId}).");
                return false;
            }

            var payload = new Dictionary<string, object>
            {
                { "mode", mode.ToString().ToLowerInvariant() },
                { "runId", activeRunId },
                { "runStart", activeRunStartUnix },
                { "runEnd", activeRunEndUnix }
            };

            var callable = functions.GetHttpsCallable(FunctionName);
            var result = await callable.CallAsync(payload);

            submittedRunIds.Add(activeRunId);
            Debug.Log($"Weekly win submitted successfully. runId={activeRunId}, response={result.Data}");
            return true;
        }
        catch (FunctionsException exception)
        {
            Debug.LogError(
                $"Weekly win rejected by Cloud Function. Code={exception.ErrorCode}, Message={exception.Message}, Details={exception.Details}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Weekly win submission failed unexpectedly: {exception}");
            return false;
        }
    }
}
