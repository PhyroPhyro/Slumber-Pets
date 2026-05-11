using Godot;
using System;
public partial class SleepModeScene : Control
{
private Label _statusLabel;
private Label _timerLabel;
private Label _movementLabel;
private SleepTracker _sleepTracker;
private Timer _updateTimer;
public override void _Ready()
{
_statusLabel = GetNode<Label>("VBox/StatusLabel");
_timerLabel = GetNode<Label>("VBox/TimerLabel");
_movementLabel = GetNode<Label>("VBox/MovementLabel");
var wakeButton = GetNode<Button>("VBox/WakeButton");
wakeButton.Pressed += OnWakePressed;
_sleepTracker = GetNode<SleepTracker>("/root/SleepTracker");
_sleepTracker.OnSleepStarted += OnSleepStarted;
_sleepTracker.OnSleepEnded += OnSleepEnded;
_updateTimer = new Timer();
_updateTimer.WaitTime = 1.0f;
_updateTimer.Timeout += OnUpdateTimer;
AddChild(_updateTimer);
_updateTimer.Start();
_sleepTracker.StartSleep();
}
private void OnSleepStarted()
{
_statusLabel.Text = "Dormindo...";
}
private void OnSleepEnded(Variant reportVariant)
{
var report = JsonSerialization.FromJson<SleepReportData>(reportVariant.AsString());
var gameManager = GetNode<GameManager>("/root/GameManager");
gameManager.AddStarCoins(report.CoinsEarned);
gameManager.CurrentSave.SleepHistory.Add(report);
gameManager.SaveGame();
GD.Print("[SleepModeScene] Sleep report saved. Coins earned: ", report.CoinsEarned);
GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
}
private void OnUpdateTimer()
{
if (_sleepTracker.IsSleeping)
{
TimeSpan elapsed = _sleepTracker.GetElapsedSleepTime();
_timerLabel.Text = elapsed.ToString(@"hh\:mm\:ss");
_movementLabel.Text = "Movimentos: " + _sleepTracker.GetMovementCount();
}
}
private void OnWakePressed()
{
_sleepTracker.EndSleep();
}
}