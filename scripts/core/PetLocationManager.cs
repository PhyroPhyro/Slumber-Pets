using Godot;
using System;
public enum PetLocation
{
PC,
Mobile
}
public enum PetTransferState
{
None,
Sending,
Receiving,
Failed,
Unknown
}
public partial class PetLocationManager : Node
{
[Signal] public delegate void OnPetSentEventHandler();
[Signal] public delegate void OnPetReceivedEventHandler();
[Signal] public delegate void OnTransferFailedEventHandler(string reason);
[Signal] public delegate void OnLocationChangedEventHandler(PetLocation newLocation);
private PetLocation _currentLocation = PetLocation.PC;
private PetTransferState _transferState = PetTransferState.None;
private string _transferId = string.Empty;
private Timer _transitTimeoutTimer;
private const float TransitTimeoutSeconds = 30.0f;
public PetLocation CurrentLocation => _currentLocation;
public PetTransferState TransferState => _transferState;
public string TransferId => _transferId;
public bool IsTransferring => _transferState == PetTransferState.Sending || _transferState == PetTransferState.Receiving || _transferState == PetTransferState.Unknown;
public override void _Ready()
{
GD.Print("[PetLocationManager] Initialized. Pet location: ", _currentLocation);
}
public void SetLocation(PetLocation location)
{
if (_transferState != PetTransferState.None && _transferState != PetTransferState.Failed)
{
GD.PrintErr("[PetLocationManager] Cannot change location while transferring");
return;
}
PetLocation oldLocation = _currentLocation;
_currentLocation = location;
GD.Print("[PetLocationManager] Pet location changed: ", oldLocation, " -> ", _currentLocation);
EmitSignal(SignalName.OnLocationChanged, Variant.From((int)_currentLocation));
}
public void StartTransfer(string transferId)
{
_transferState = PetTransferState.Sending;
_transferId = transferId;
GD.Print("[PetLocationManager] Transfer started: ", transferId);
StartTransitTimeout();
}

private void StartTransitTimeout()
{
_transitTimeoutTimer?.Stop();
_transitTimeoutTimer?.QueueFree();
_transitTimeoutTimer = new Timer();
_transitTimeoutTimer.WaitTime = TransitTimeoutSeconds;
_transitTimeoutTimer.OneShot = true;
_transitTimeoutTimer.Timeout += OnTransitTimeout;
AddChild(_transitTimeoutTimer);
_transitTimeoutTimer.Start();
GD.Print("[PetLocationManager] Transit timeout started: ", TransitTimeoutSeconds, "s");
}

private void StopTransitTimeout()
{
_transitTimeoutTimer?.Stop();
_transitTimeoutTimer?.QueueFree();
_transitTimeoutTimer = null;
}

private void OnTransitTimeout()
{
if (_transferState == PetTransferState.Sending || _transferState == PetTransferState.Receiving)
{
GD.PrintErr("[PetLocationManager] Transit timeout reached, setting state to Unknown");
SetUnknownState("Transfer timed out without confirmation");
}
}

public void SetUnknownState(string reason)
{
_transferState = PetTransferState.Unknown;
GD.PrintErr("[PetLocationManager] Transfer state set to Unknown: ", reason);
EmitSignal(SignalName.OnTransferFailed, reason);
}
    public void CompleteTransfer(PetLocation newLocation)
    {
        StopTransitTimeout();
        _transferState = PetTransferState.None;
        _transferId = string.Empty;
        _currentLocation = newLocation;
        GD.Print("[PetLocationManager] Transfer completed. New location: ", _currentLocation);
        EmitSignal(SignalName.OnPetSent);
    }

    public void ReceiveTransfer(PetLocation newLocation)
    {
        StopTransitTimeout();
        _transferState = PetTransferState.None;
        _transferId = string.Empty;
        _currentLocation = newLocation;
        GD.Print("[PetLocationManager] Transfer received. New location: ", _currentLocation);
        EmitSignal(SignalName.OnPetReceived);
    }
    public void FailTransfer(string reason)
    {
        StopTransitTimeout();
        _transferState = PetTransferState.Failed;
        GD.PrintErr("[PetLocationManager] Transfer failed: ", reason);
        EmitSignal(SignalName.OnTransferFailed, reason);
    }
public bool CanInteract()
{
return !IsTransferring;
}
public bool IsPetAtCurrentPlatform(PetLocation platform)
{
return _currentLocation == platform && !IsTransferring;
}
}