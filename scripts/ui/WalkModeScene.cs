using Godot;
using System;
public partial class WalkModeScene : Control
{
private Label _stepsLabel;
private Label _milestonesLabel;
private Label _itemFoundLabel;
private WalkTracker _walkTracker;
public override void _Ready()
{
_stepsLabel = GetNode<Label>("VBox/StepsLabel");
_milestonesLabel = GetNode<Label>("VBox/MilestonesLabel");
_itemFoundLabel = GetNode<Label>("VBox/ItemFoundLabel");
var endButton = GetNode<Button>("VBox/EndWalkButton");
endButton.Pressed += OnEndWalkPressed;
_walkTracker = GetNode<WalkTracker>("/root/WalkTracker");
_walkTracker.OnWalkStarted += OnWalkStarted;
_walkTracker.OnWalkEnded += OnWalkEnded;
_walkTracker.OnItemFound += OnItemFound;
_walkTracker.StartWalk();
}
private void OnWalkStarted()
{
_stepsLabel.Text = "Passos: 0";
_milestonesLabel.Text = "Marcos: 0";
}
private void OnWalkEnded(Variant reportVariant)
{
var report = JsonSerialization.FromJson<WalkReportData>(reportVariant.AsString());
var gameManager = GetNode<GameManager>("/root/GameManager");
gameManager.CurrentSave.WalkHistory.Add(report);
gameManager.SaveGame();
GD.Print("[WalkModeScene] Walk report saved. Steps: ", report.Steps);
GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
}
private void OnItemFound(Variant itemVariant)
{
var item = JsonSerialization.FromJson<ItemDrop>(itemVariant.AsString());
_itemFoundLabel.Text = "Item encontrado: " + item.Name + "!";
}
private void OnEndWalkPressed()
{
_walkTracker.EndWalk();
}
public override void _Process(double delta)
{
if (_walkTracker.IsWalking)
{
_stepsLabel.Text = "Passos: " + _walkTracker.CurrentSteps;
var milestoneManager = GetNode<MilestoneManager>("/root/MilestoneManager");
_milestonesLabel.Text = "Marcos: " + milestoneManager.MilestonesReachedToday;
}
}
}