using Godot;
using System;

public partial class PetSendScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private Timer _searchTimeoutTimer;
    private const float SearchTimeoutSeconds = 30.0f;

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

        _searchTimeoutTimer = new Timer();
        _searchTimeoutTimer.WaitTime = SearchTimeoutSeconds;
        _searchTimeoutTimer.OneShot = true;
        _searchTimeoutTimer.Timeout += OnSearchTimeout;
        AddChild(_searchTimeoutTimer);
        _searchTimeoutTimer.Start();

        GD.Print("[PetSendScene] Broadcasting as host... (timeout: ", SearchTimeoutSeconds, "s)");
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Enviando pet...";
        _searchTimeoutTimer.Stop();
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
        _searchTimeoutTimer.Stop();
    }

    private void OnSearchTimeout()
    {
        _statusLabel.Text = "Tempo esgotado - Mobile não conectou. Verifique se ambos estão na mesma rede.";
        GD.PrintErr("[PetSendScene] Timeout waiting for mobile connection");
        _transferManager.StopTransfer();
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        _searchTimeoutTimer.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }
}
