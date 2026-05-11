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
var report = new SleepReportData
{
Date = _sleepStartTime.ToString("yyyy-MM-dd"),
DurationMin = durationMin,
Score = score,
Tier = tier,
CoinsEarned = coinsEarned
};
EmitSignal(SignalName.OnSleepEnded, JsonSerialization.ToJson(report));
GD.Print("[SleepTracker] Sleep ended. Duration: ", durationMin, "min, Quality: ", tier, ", Coins: ", coinsEarned);
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
public int GetMovementCount() => _movementCount;
public TimeSpan GetElapsedSleepTime() => _isSleeping ? DateTime.UtcNow - _sleepStartTime : TimeSpan.Zero;
}
public enum SleepQuality
{
Excellent,
Good,
Regular,
Poor
}