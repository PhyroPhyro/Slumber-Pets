using Godot;
using System;
public partial class IdleIncomeManager : Node
{
[Signal] public delegate void OnIdleIncomeTickEventHandler(int amount);
[Signal] public delegate void OnCoinsPerMinuteChangedEventHandler(float newRate);
private Timer _incomeTimer;
private BalanceConfig _balanceConfig;
private int _sessionTotalCoins = 0;
private float _coinsPerMinute = 0f;
public float MoedasPorMinutoAtual => _coinsPerMinute;
public int SessionTotalCoins => _sessionTotalCoins;
public override void _Ready()
{
LoadBalanceConfig();
_incomeTimer = new Timer();
_incomeTimer.WaitTime = _balanceConfig.TickIntervalSeconds;
_incomeTimer.OneShot = false;
_incomeTimer.Timeout += OnIncomeTick;
AddChild(_incomeTimer);
_incomeTimer.Start();
GD.Print("[IdleIncomeManager] Initialized. Tick interval: ", _balanceConfig.TickIntervalSeconds, "s");
}
private void LoadBalanceConfig()
{
string path = "res://data/balance.json";
if (!FileAccess.FileExists(path))
{
GD.PrintErr("[IdleIncomeManager] balance.json not found, using defaults");
_balanceConfig = new BalanceConfig();
return;
}
try
{
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
_balanceConfig = JsonSerialization.FromJson<BalanceConfig>(json) ?? new BalanceConfig();
}
catch (Exception e)
{
GD.PrintErr("[IdleIncomeManager] Error loading balance.json: ", e.Message);
_balanceConfig = new BalanceConfig();
}
}
private void OnIncomeTick()
{
var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
if (petLocationManager.CurrentLocation != PetLocation.PC || petLocationManager.IsTransferring)
{
_coinsPerMinute = 0f;
EmitSignal(SignalName.OnCoinsPerMinuteChanged, _coinsPerMinute);
return;
}
var gameManager = GetNode<GameManager>("/root/GameManager");
var resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
PetState petState = gameManager.GetPetState();
float baseRate = CalculateBaseRate(petState.Energy);
float happinessBonus = petState.Happiness >= _balanceConfig.HappinessThreshold ? _balanceConfig.HappinessBonus : 0f;
float hungerPenalty = petState.Hunger <= _balanceConfig.HungerThreshold ? _balanceConfig.HungerPenalty : 0f;
_coinsPerMinute = baseRate + happinessBonus - hungerPenalty;
_coinsPerMinute = Mathf.Max(_coinsPerMinute, 0f);
int coinsToAward = Mathf.RoundToInt(_coinsPerMinute * (_balanceConfig.TickIntervalSeconds / 60f));
if (coinsToAward > 0)
{
resourceManager.AddCoins(coinsToAward);
_sessionTotalCoins += coinsToAward;
EmitSignal(SignalName.OnIdleIncomeTick, coinsToAward);
}
EmitSignal(SignalName.OnCoinsPerMinuteChanged, _coinsPerMinute);
}
private float CalculateBaseRate(float energy)
{
if (energy >= _balanceConfig.EnergyThresholds.High)
return _balanceConfig.BaseIncomeRates.EnergyHigh;
if (energy >= _balanceConfig.EnergyThresholds.Medium)
return _balanceConfig.BaseIncomeRates.EnergyMedium;
return _balanceConfig.BaseIncomeRates.EnergyLow;
}
public void ResetSession()
{
_sessionTotalCoins = 0;
}
}
public class BalanceConfig
{
public IncomeRates BaseIncomeRates { get; set; } = new();
public float HappinessBonus { get; set; } = 0.5f;
public float HungerPenalty { get; set; } = 0.5f;
public EnergyThresholdsConfig EnergyThresholds { get; set; } = new();
public int HungerThreshold { get; set; } = 20;
public int HappinessThreshold { get; set; } = 70;
public int TickIntervalSeconds { get; set; } = 60;
}
public class IncomeRates
{
public float EnergyHigh { get; set; } = 2.0f;
public float EnergyMedium { get; set; } = 1.0f;
public float EnergyLow { get; set; } = 0.5f;
}
public class EnergyThresholdsConfig
{
public int High { get; set; } = 70;
public int Medium { get; set; } = 40;
}