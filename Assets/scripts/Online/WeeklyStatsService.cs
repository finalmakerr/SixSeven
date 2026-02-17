using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using UnityEngine;

public class WeeklyStatsService
{
    private const string StartRunFunctionName = "startWeeklyRun";
    private const string CompleteRunFunctionName = "completeWeeklyRun";

    private FirebaseAuth auth;
    private FirebaseFunctions functions;
    private bool isInitialized;

    private bool isLeaderboardEligible;
    private string activeRunId;

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

    public async Task<bool> StartLeaderboardRunAsync(GameMode mode)
    {
        try
        {
            await Initialize();

            if (!isInitialized || auth == null || auth.CurrentUser == null || functions == null)
            {
                Debug.LogError("StartLeaderboardRunAsync failed: service is not initialized or authenticated.");
                isLeaderboardEligible = false;
                return false;
            }

            var payload = new Dictionary<string, object>
            {
                { "mode", mode.ToString().ToLowerInvariant() }
            };

            var callable = functions.GetHttpsCallable(StartRunFunctionName);
            var result = await callable.CallAsync(payload);
            var response = result.Data as IDictionary;

            if (response == null || !response.Contains("success") || !(response["success"] is bool success) || !success)
            {
                Debug.LogWarning($"StartLeaderboardRunAsync rejected: response={result.Data}");
                isLeaderboardEligible = false;
                return false;
            }

            if (!response.Contains("runId") || string.IsNullOrWhiteSpace(response["runId"]?.ToString()))
            {
                Debug.LogWarning("StartLeaderboardRunAsync failed: Cloud Function did not return a valid runId.");
                isLeaderboardEligible = false;
                return false;
            }

            activeRunId = response["runId"].ToString();
            isLeaderboardEligible = true;
            Debug.Log($"Leaderboard run started. runId={activeRunId}");
            return true;
        }
        catch (FunctionsException exception)
        {
            Debug.LogError(
                $"StartLeaderboardRunAsync rejected by Cloud Function. Code={exception.ErrorCode}, Message={exception.Message}, Details={exception.Details}");
            isLeaderboardEligible = false;
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"StartLeaderboardRunAsync failed unexpectedly: {exception}");
            isLeaderboardEligible = false;
            return false;
        }
    }

    public async Task<bool> SubmitWeeklyRunCompletionAsync(GameMode mode)
    {
        try
        {
            await Initialize();

            if (!isInitialized)
            {
                Debug.LogError("SubmitWeeklyRunCompletionAsync failed: service is not initialized.");
                return false;
            }

            if (auth == null || auth.CurrentUser == null)
            {
                Debug.LogError("SubmitWeeklyRunCompletionAsync failed: user is not authenticated.");
                return false;
            }

            if (!isLeaderboardEligible)
            {
                Debug.Log("SubmitWeeklyRunCompletionAsync skipped: current run is not leaderboard eligible.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(activeRunId))
            {
                Debug.LogError("SubmitWeeklyRunCompletionAsync failed: runId is missing. Call StartLeaderboardRunAsync first.");
                return false;
            }

            var payload = new Dictionary<string, object>
            {
                { "mode", mode.ToString().ToLowerInvariant() },
                { "runId", activeRunId }
            };

            var callable = functions.GetHttpsCallable(CompleteRunFunctionName);
            var result = await callable.CallAsync(payload);
            var response = result.Data as IDictionary;

            if (response == null || !response.Contains("success") || !(response["success"] is bool success) || !success)
            {
                Debug.LogWarning($"Weekly run completion submission rejected. runId={activeRunId}, response={result.Data}");
                return false;
            }

            Debug.Log($"Weekly run completion submitted successfully. runId={activeRunId}, response={result.Data}");
            isLeaderboardEligible = false;
            activeRunId = null;
            return true;
        }
        catch (FunctionsException exception)
        {
            Debug.LogError(
                $"Weekly run completion rejected by Cloud Function. Code={exception.ErrorCode}, Message={exception.Message}, Details={exception.Details}");
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Weekly run completion submission failed unexpectedly: {exception}");
            return false;
        }
    }
}
