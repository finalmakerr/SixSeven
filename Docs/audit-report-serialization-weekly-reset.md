# Audit Report: Serialization + Weekly Reset Compatibility

## 1) JsonUtility usages (`JsonUtility.ToJson` / `JsonUtility.FromJson`)

_No matches found in project._

## 2) `weekStartUtc` references

### Usages expecting a string

**Assets/scripts/GameManager.cs**
```csharp
var weekStartToken = root["weeklyModeStats"]?["weekStartUtc"];
if (weekStartToken != null && weekStartToken.Type == JTokenType.String)
{
    var parsed = DateTime.Parse(
        weekStartToken.Value<string>(),
        null,
        System.Globalization.DateTimeStyles.RoundtripKind);
    root["weeklyModeStats"]["weekStartUtc"] = JToken.FromObject(parsed);
}
```

### Usages performing `DateTime.TryParse`

_No matches found in project._

## 3) `WeeklyModeStats` initialization safety (`PlayerProfile` construction/deserialization)

### Construction/default initialization

**Assets/scripts/Profile/PlayerProfile.cs**
```csharp
public WeeklyModeStats weeklyModeStats = new WeeklyModeStats();
```

**Assets/scripts/GameManager.cs**
```csharp
[SerializeField] private PlayerProfile profile = new PlayerProfile();
...
if (string.IsNullOrWhiteSpace(raw))
{
    profile = new PlayerProfile();
    return;
}
...
profile = JsonConvert.DeserializeObject<PlayerProfile>(root.ToString());
if (profile == null)
    profile = new PlayerProfile();
```

### Post-deserialization null-guard

**Assets/scripts/GameManager.cs**
```csharp
if (profile.weeklyModeStats == null)
    profile.weeklyModeStats = new WeeklyModeStats();
```

## 4) Newtonsoft.Json-only serialization check

**Assets/scripts/GameManager.cs**
```csharp
var root = JObject.Parse(raw);
...
profile = JsonConvert.DeserializeObject<PlayerProfile>(root.ToString());
...
var raw = JsonConvert.SerializeObject(profile, Formatting.Indented);
```

_No `JsonUtility` serialization calls remain._

## 5) String read/write patterns for `weekStartUtc`

### String reads

**Assets/scripts/GameManager.cs**
```csharp
if (weekStartToken != null && weekStartToken.Type == JTokenType.String)
{
    var parsed = DateTime.Parse(
        weekStartToken.Value<string>(),
        null,
        System.Globalization.DateTimeStyles.RoundtripKind);
```

### String writes (`.ToString("o")`, string casts)

_No matches found in project._
