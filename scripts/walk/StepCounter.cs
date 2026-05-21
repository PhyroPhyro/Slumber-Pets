using Godot;
using System;
public partial class StepCounter : Node
{
[Signal] public delegate void OnStepDetectedEventHandler(int totalSteps);
[Signal] public delegate void OnMilestoneStepEventHandler(int steps);
private Timer _stepTimer;
private int _stepsToday = 0;
private float _lastMagnitude = 0f;
private double _lastStepTime = 0;
private const float Threshold = 10.0f;
private const float MinStepInterval = 0.35f;
private const int PersistInterval = 100;
private string _lastSaveDate = "";
public int StepsToday => _stepsToday;
public override void _Ready()
{
_stepTimer = new Timer();
_stepTimer.WaitTime = 0.05f;
_stepTimer.OneShot = false;
_stepTimer.Timeout += OnStepCheck;
AddChild(_stepTimer);
LoadStepsToday();
CheckAndResetIfNewDay();
}
public void StartCounting()
{
_stepTimer.Start();
_lastStepTime = 0;
GD.Print("[StepCounter] Started counting steps");
}
public void StopCounting()
{
_stepTimer.Stop();
SaveStepsToday();
GD.Print("[StepCounter] Stopped counting. Total: ", _stepsToday);
}
private void OnStepCheck()
{
Vector3 accel = Input.GetAccelerometer();
float magnitude = accel.Length();
double currentTime = Time.GetTicksMsec() / 1000.0;
bool isAboveThreshold = magnitude > Threshold;
bool isEnoughTimePassed = (currentTime - _lastStepTime) > MinStepInterval;
if (isAboveThreshold && isEnoughTimePassed)
{
_stepsToday++;
_lastStepTime = currentTime;
EmitSignal(SignalName.OnStepDetected, _stepsToday);
if (_stepsToday % PersistInterval == 0)
{
SaveStepsToday();
}
int[] milestones = { 500, 1000, 2000, 3500, 5000 };
foreach (int milestone in milestones)
{
if (_stepsToday == milestone)
{
EmitSignal(SignalName.OnMilestoneStep, _stepsToday);
break;
}
}
}
_lastMagnitude = magnitude;
}
public void ResetDailySteps()
{
_stepsToday = 0;
SaveStepsToday();
GD.Print("[StepCounter] Daily steps reset");
}
private void SaveStepsToday()
{
string path = "user://saves/steps_today.dat";
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
file.StoreVar(_stepsToday);
file.StoreVar(_lastSaveDate);
}
private void LoadStepsToday()
{
string path = "user://saves/steps_today.dat";
if (FileAccess.FileExists(path))
{
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
_stepsToday = (int)file.GetVar();
Variant dateVar = file.GetVar();
_lastSaveDate = dateVar.Obj as string ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
}
else
{
_lastSaveDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
}
}
private void CheckAndResetIfNewDay()
{
string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
if (_lastSaveDate != today)
{
GD.Print("[StepCounter] New day detected, resetting steps. Last: ", _lastSaveDate, " Today: ", today);
_stepsToday = 0;
_lastSaveDate = today;
SaveStepsToday();
}
}
}