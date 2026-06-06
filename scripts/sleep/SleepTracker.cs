using Godot;
using System;
using System.Collections.Generic;
public partial class SleepTracker : Node
{
[Signal] public delegate void OnSleepStartedEventHandler();
[Signal] public delegate void OnSleepEndedEventHandler(Variant report);
private AccelerometerReader _accelerometerReader;
private DateTime _sleepStartTime;
private int _movementCount = 0;
private bool _isSleeping = false;
public bool IsSleeping => _isSleeping;
public override void _Ready()
{
_accelerometerReader = GetNode<AccelerometerReader>("/root/AccelerometerReader");
_accelerometerReader.OnMovementDetected += OnMovement;
}
public void StartSleep()
{
if (_isSleeping) return;
_isSleeping = true;
_sleepStartTime = DateTime.UtcNow;
_movementCount = 0;
// KeepScreenOn not available in Godot 4.4 C# yet
_accelerometerReader.StartReading();
EmitSignal(SignalName.OnSleepStarted);
GD.Print("[SleepTracker] Sleep mode started at ", _sleepStartTime);
}
public void EndSleep()
{
if (!_isSleeping) return;
_isSleeping = false;
DateTime endTime = DateTime.UtcNow;
// KeepScreenOn not available in Godot 4.4 C# yet
_accelerometerReader.StopReading();
int durationMin = (int)(endTime - _sleepStartTime).TotalMinutes;
float movementsPerHour = durationMin > 0 ? (_movementCount / (durationMin / 60f)) : 0;
SleepQuality quality = CalculateQuality(durationMin, movementsPerHour);
int score = CalculateScore(durationMin, movementsPerHour);
    int coinsEarned = CalculateCoins(durationMin, quality);
    string tier = GetTier(quality);
    bool midnightBonus = ApplyMidnightBonus();
    float consistencyBonus = CalculateConsistencyBonus();
    float sleepBoost = GetSleepQualityMultiplier();
    int totalCoins = coinsEarned;
    if (midnightBonus) totalCoins = (int)(totalCoins * 1.2f);
    if (consistencyBonus > 0f) totalCoins = (int)(totalCoins * (1f + consistencyBonus));
    if (sleepBoost > 1f) totalCoins = (int)(totalCoins * sleepBoost);
    var report = new SleepReportData
    {
        Date = _sleepStartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
        DurationMin = durationMin,
        Score = score,
        Tier = tier,
        CoinsEarned = totalCoins,
        MidnightBonus = midnightBonus,
        ConsistencyBonus = consistencyBonus
    };
EmitSignal(SignalName.OnSleepEnded, JsonSerialization.ToJson(report));
GD.Print("[SleepTracker] Sleep ended. Duration: ", durationMin, "min, Quality: ", tier, ", Coins: ", totalCoins, " (base: ", coinsEarned, ")");
}
private void OnMovement(float magnitude)
{
if (_isSleeping)
{
_movementCount++;
}
}
private SleepQuality CalculateQuality(int durationMin, float movementsPerHour)
{
if (movementsPerHour < 5 && durationMin >= 420 && durationMin <= 540)
return SleepQuality.Excellent;
if (movementsPerHour <= 15 && durationMin >= 360 && durationMin <= 540)
return SleepQuality.Good;
if (movementsPerHour <= 30 || durationMin < 360)
return SleepQuality.Regular;
return SleepQuality.Poor;
}
private int CalculateScore(int durationMin, float movementsPerHour)
{
int score = 0;
if (durationMin >= 420 && durationMin <= 540) score += 40;
else if (durationMin >= 360) score += 30;
else if (durationMin >= 240) score += 20;
else score += 10;
if (movementsPerHour < 5) score += 30;
else if (movementsPerHour < 15) score += 20;
else if (movementsPerHour < 30) score += 10;
DateTime start = _sleepStartTime;
if (start.Hour < 24) score += 15;
return Math.Min(score, 100);
}
private int CalculateCoins(int durationMin, SleepQuality quality)
{
float multiplier = quality switch
{
SleepQuality.Excellent => 2.0f,
SleepQuality.Good => 1.5f,
SleepQuality.Regular => 1.0f,
SleepQuality.Poor => 0.5f,
_ => 1.0f
};
return Mathf.RoundToInt(durationMin * multiplier);
}
private string GetTier(SleepQuality quality)
{
return quality switch
{
SleepQuality.Excellent => "Excellent",
SleepQuality.Good => "Good",
SleepQuality.Regular => "Regular",
SleepQuality.Poor => "Poor",
_ => "Unknown"
};
}
    private bool ApplyMidnightBonus()
    {
        DateTime localStart = _sleepStartTime.ToLocalTime();
        return localStart.Hour >= 0 && localStart.Hour < 6;
    }
private float CalculateConsistencyBonus()
{
var gameManager = GetNode<GameManager>("/root/GameManager");
var sleepHistory = gameManager.CurrentSave.SleepHistory;
if (sleepHistory.Count < 7) return 0f;
var last7 = sleepHistory.GetRange(Math.Max(0, sleepHistory.Count - 7), Math.Min(7, sleepHistory.Count));
if (last7.Count < 7) return 0f;
    List<int> startHours = new List<int>();
    foreach (var report in last7)
    {
        if (DateTime.TryParse(report.Date, out DateTime sleepTime))
        {
            startHours.Add(sleepTime.Hour);
        }
    }
if (startHours.Count < 7) return 0f;
float mean = 0f;
foreach (int h in startHours) mean += h;
mean /= startHours.Count;
float variance = 0f;
foreach (int h in startHours) variance += (h - mean) * (h - mean);
variance /= startHours.Count;
float stdDev = Mathf.Sqrt(variance);
if (stdDev < 1.5f) return 0.15f;
return 0f;
}
    public int GetMovementCount() => _movementCount;
    public TimeSpan GetElapsedSleepTime() => _isSleeping ? DateTime.UtcNow - _sleepStartTime : TimeSpan.Zero;

    private float GetSleepQualityMultiplier()
    {
        var effectManager = GetNode<ItemEffectManager>("/root/ItemEffectManager");
        return effectManager.GetSleepQualityMultiplier();
    }
}
public enum SleepQuality
{
Excellent,
Good,
Regular,
Poor
}