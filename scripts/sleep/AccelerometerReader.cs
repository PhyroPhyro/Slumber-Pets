using Godot;
using System;
public partial class AccelerometerReader : Node
{
[Signal] public delegate void OnMovementDetectedEventHandler(float magnitude);
private Timer _readTimer;
private float _lastMagnitude = 0f;
public float CurrentMagnitude => _lastMagnitude;
public override void _Ready()
{
_readTimer = new Timer();
_readTimer.WaitTime = 0.5f;
_readTimer.OneShot = false;
_readTimer.Timeout += OnReadTick;
AddChild(_readTimer);
}
public void StartReading()
{
_readTimer.Start();
GD.Print("[AccelerometerReader] Started reading accelerometer");
}
public void StopReading()
{
_readTimer.Stop();
GD.Print("[AccelerometerReader] Stopped reading accelerometer");
}
private void OnReadTick()
{
Vector3 accel = Input.GetAccelerometer();
float magnitude = accel.Length();
_lastMagnitude = magnitude;
if (magnitude > 1.1f)
{
EmitSignal(SignalName.OnMovementDetected, magnitude);
}
}
public Vector3 GetCurrentReading()
{
return Input.GetAccelerometer();
}
}