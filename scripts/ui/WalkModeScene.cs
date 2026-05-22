using Godot;
using System;
public partial class WalkModeScene : Control
{
private Label _stepsLabel;
private Label _milestonesLabel;
private Label _itemFoundLabel;
private WalkTracker _walkTracker;
private ItemFoundScene _itemFoundOverlay;
public override void _Ready()
{
_stepsLabel = GetNode<Label>("VBox/StepsLabel");
_milestonesLabel = GetNode<Label>("VBox/MilestonesLabel");
_itemFoundLabel = GetNode<Label>("VBox/ItemFoundLabel");
_itemFoundOverlay = GetNodeOrNull<ItemFoundScene>("ItemFoundOverlay");
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

        WalkReportScene.SetPendingReport(report);
        GetTree().ChangeSceneToFile("res://scenes/walk/walk_report.tscn");
    }
private async void OnItemFound(Variant itemVariant)
{
var item = JsonSerialization.FromJson<ItemDrop>(itemVariant.AsString());
_itemFoundLabel.Text = "Item encontrado: " + item.Name + "!";
if (_itemFoundOverlay != null)
{
_itemFoundOverlay.ShowItem(item.Id, item.Name, item.Rarity);
}
else
{
await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
_itemFoundLabel.Text = "";
}
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