using Godot;
using System;
public partial class MainMobile : Control
{
private Label _statusLabel;
public override void _Ready()
{
_statusLabel = GetNode<Label>("VBox/StatusLabel");
var sleepButton = GetNode<Button>("VBox/Buttons/SleepButton");
var walkButton = GetNode<Button>("VBox/Buttons/WalkButton");
var returnButton = GetNode<Button>("VBox/Buttons/ReturnButton");
sleepButton.Pressed += OnSleepPressed;
walkButton.Pressed += OnWalkPressed;
returnButton.Pressed += OnReturnPressed;
}
private void DisableActions()
{
var sleepButton = GetNode<Button>("%SleepButton");
var walkButton = GetNode<Button>("%WalkButton");
var returnButton = GetNode<Button>("%ReturnButton");
sleepButton.Disabled = true;
walkButton.Disabled = true;
returnButton.Disabled = true;
}
private void OnSleepPressed()
{
GetTree().ChangeSceneToFile("res://scenes/sleep/sleep_mode.tscn");
}
private void OnWalkPressed()
{
GetTree().ChangeSceneToFile("res://scenes/walk/walk_mode.tscn");
}
private void OnReturnPressed()
{
GD.Print("[MainMobile] Return to PC pressed - LAN transfer not implemented yet");
}
}