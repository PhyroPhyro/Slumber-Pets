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
string rareItemId = RollRareItem(report.Tier);
if (!string.IsNullOrEmpty(rareItemId))
{
var inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
inventoryManager.AddItem(rareItemId, 1);
report.RareItemId = rareItemId;
GD.Print("[SleepModeScene] Rare item awarded: ", rareItemId);
}
gameManager.CurrentSave.SleepHistory.Add(report);
gameManager.SaveGame();
GD.Print("[SleepModeScene] Sleep report saved. Coins earned: ", report.CoinsEarned);
GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
}
private string RollRareItem(string tier)
{
float rareChance = tier switch
{
"Excellent" => 100f,
"Good" => 50f,
"Regular" => 20f,
_ => 5f
};
var rng = new RandomNumberGenerator();
rng.Randomize();
if (rng.RandiRange(1, 100) > rareChance) return null;
var itemRoller = GetNode<ItemRoller>("/root/ItemRoller");
var rareItems = GetRareItems();
if (rareItems.Count == 0) return null;
var chosenItem = rareItems[rng.RandiRange(0, rareItems.Count - 1)];
return chosenItem.Id;
}
private System.Collections.Generic.List<ItemEntry> GetRareItems()
{
string path = "res://data/items.json";
if (!FileAccess.FileExists(path)) return new System.Collections.Generic.List<ItemEntry>();
try
{
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var data = JsonSerialization.FromJson<ItemsData>(json);
if (data != null && data.Items != null)
{
return data.Items.FindAll(i => i.Rarity == "rare" || i.Rarity == "rare+");
}
}
catch (Exception e)
{
GD.PrintErr("[SleepModeScene] Error loading items for rare roll: ", e.Message);
}
return new System.Collections.Generic.List<ItemEntry>();
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