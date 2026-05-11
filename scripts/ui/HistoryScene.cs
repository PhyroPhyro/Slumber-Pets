using Godot;
using System;
public partial class HistoryScene : Control
{
private Label _sleepHistoryLabel;
private Label _walkHistoryLabel;
public override void _Ready()
{
_sleepHistoryLabel = GetNode<Label>("MarginContainer/VBox/SleepHistoryLabel");
_walkHistoryLabel = GetNode<Label>("MarginContainer/VBox/WalkHistoryLabel");
var backButton = GetNode<Button>("MarginContainer/VBox/BackButton");
backButton.Pressed += OnBackPressed;
var gameManager = GetNode<GameManager>("/root/GameManager");
var save = gameManager.CurrentSave;
if (save.SleepHistory.Count > 0)
{
_sleepHistoryLabel.Text = "Sono: " + save.SleepHistory.Count + " registros";
}
else
{
_sleepHistoryLabel.Text = "Sono: Nenhum registro";
}
if (save.WalkHistory.Count > 0)
{
_walkHistoryLabel.Text = "Caminhada: " + save.WalkHistory.Count + " registros";
}
else
{
_walkHistoryLabel.Text = "Caminhada: Nenhum registro";
}
}
private void OnBackPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}
