# Weekly Win Submission Example Usage Tests

```csharp
// 1) Valid win submission
var service = new WeeklyStatsService();
await service.Initialize();
service.StartRun();
await Task.Delay(TimeSpan.FromSeconds(301));
service.EndRun();
bool validResult = await service.SubmitWeeklyWinAsync(GameMode.Normal);
Debug.Log($"Valid submission expected true, actual: {validResult}");
```

```csharp
// 2) Duplicate runId
var service = new WeeklyStatsService();
await service.Initialize();
service.StartRun();
await Task.Delay(TimeSpan.FromSeconds(301));
service.EndRun();
bool firstSubmit = await service.SubmitWeeklyWinAsync(GameMode.Hardcore);
bool duplicateSubmit = await service.SubmitWeeklyWinAsync(GameMode.Hardcore);
Debug.Log($"Duplicate submission expected false on second call, first={firstSubmit}, second={duplicateSubmit}");
```

```csharp
// 3) Run too short
var service = new WeeklyStatsService();
await service.Initialize();
service.StartRun();
await Task.Delay(TimeSpan.FromSeconds(10));
service.EndRun();
bool shortRunResult = await service.SubmitWeeklyWinAsync(GameMode.Ironman);
Debug.Log($"Short run expected false, actual: {shortRunResult}");
```
