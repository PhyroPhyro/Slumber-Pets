using Godot;
using System;
public partial class PetSendScene : Control
{
private Label _statusLabel;
private PetTransferManager _transferManager;
public override void _Ready()
{
_statusLabel = GetNode<Label>("VBox/StatusLabel");
_transferManager = GetNode<PetTransferManager>("/root/PetTransferManager");
_transferManager.OnTransferStarted += OnTransferStarted;
_transferManager.OnTransferCompleted += OnTransferCompleted;
_transferManager.OnTransferFailed += OnTransferFailed;
var cancelButton = GetNode<Button>("VBox/CancelButton");
cancelButton.Pressed += OnCancelPressed;
_transferManager.StartSendingAsHost("1234");
}
private void OnTransferStarted()
{
_statusLabel.Text = "Enviando pet...";
}
private async void OnTransferCompleted()
{
_statusLabel.Text = "Pet enviado com sucesso!";
var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
petLocationManager.SetLocation(PetLocation.Mobile);
var gameManager = GetNode<GameManager>("/root/GameManager");
gameManager.SaveGame();
_transferManager.StopTransfer();
await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
private void OnTransferFailed(string reason)
{
_statusLabel.Text = "Falha na transferência: " + reason;
_transferManager.StopTransfer();
}
private void OnCancelPressed()
{
_transferManager.StopTransfer();
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}