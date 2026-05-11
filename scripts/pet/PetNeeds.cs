using Godot;
using System;
public partial class PetNeeds : Node
{
[Signal] public delegate void OnHungerChangedEventHandler(float newValue);
[Signal] public delegate void OnHappinessChangedEventHandler(float newValue);
[Signal] public delegate void OnEnergyChangedEventHandler(float newValue);
private PetState _petState;
private Timer _decayTimer;
private const float DecayIntervalSeconds = 60f;
public PetState CurrentState => _petState;
public override void _Ready()
{
_decayTimer = new Timer();
_decayTimer.WaitTime = DecayIntervalSeconds;
_decayTimer.OneShot = false;
_decayTimer.Timeout += OnDecayTick;
AddChild(_decayTimer);
_decayTimer.Start();
GD.Print("[PetNeeds] Initialized with decay timer every ", DecayIntervalSeconds, "s");
}
public void Initialize(PetState state)
{
_petState = state;
EmitSignals();
}
private void OnDecayTick()
{
if (_petState == null) return;
double minutesElapsed = DecayIntervalSeconds / 60.0;
double hoursElapsed = minutesElapsed / 60.0;
float hungerDecay = 10f * (float)hoursElapsed;
float happinessDecay = 5f * (float)hoursElapsed;
float energyDecay = 15f * (float)hoursElapsed;
_petState.Hunger = Mathf.Clamp(_petState.Hunger - hungerDecay, 0f, 100f);
_petState.Happiness = Mathf.Clamp(_petState.Happiness - happinessDecay, 0f, 100f);
_petState.Energy = Mathf.Clamp(_petState.Energy - energyDecay, 0f, 100f);
_petState.LastUpdatedAt = DateTime.UtcNow;
EmitSignals();
}
public void Feed(float hungerRestore, float happinessRestore = 0f)
{
if (_petState == null) return;
_petState.Hunger = Mathf.Clamp(_petState.Hunger + hungerRestore, 0f, 100f);
_petState.Happiness = Mathf.Clamp(_petState.Happiness + happinessRestore, 0f, 100f);
_petState.LastUpdatedAt = DateTime.UtcNow;
EmitSignals();
}
public void Play(float happinessRestore)
{
if (_petState == null) return;
_petState.Happiness = Mathf.Clamp(_petState.Happiness + happinessRestore, 0f, 100f);
_petState.LastUpdatedAt = DateTime.UtcNow;
EmitSignals();
}
public void Rest(float energyRestore)
{
if (_petState == null) return;
_petState.Energy = Mathf.Clamp(_petState.Energy + energyRestore, 0f, 100f);
_petState.LastUpdatedAt = DateTime.UtcNow;
EmitSignals();
}
public void ApplySleepReport(int energyRestore, float happinessBonus = 0f)
{
if (_petState == null) return;
_petState.Energy = Mathf.Clamp(_petState.Energy + energyRestore, 0f, 100f);
_petState.Happiness = Mathf.Clamp(_petState.Happiness + happinessBonus, 0f, 100f);
_petState.LastUpdatedAt = DateTime.UtcNow;
EmitSignals();
}
private void EmitSignals()
{
if (_petState == null) return;
EmitSignal(SignalName.OnHungerChanged, _petState.Hunger);
EmitSignal(SignalName.OnHappinessChanged, _petState.Happiness);
EmitSignal(SignalName.OnEnergyChanged, _petState.Energy);
}
public float GetHunger() => _petState?.Hunger ?? 0f;
public float GetHappiness() => _petState?.Happiness ?? 0f;
public float GetEnergy() => _petState?.Energy ?? 0f;
}