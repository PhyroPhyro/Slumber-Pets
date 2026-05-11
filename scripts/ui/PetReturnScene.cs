using Godot;
using System;

public partial class PetReturnScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private LanDiscovery _lanDiscovery;
    private bool _isConnecting = false;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _transferManager = GetNode<PetTransferManager>("/root/PetTransferManager");
        _lanDiscovery = GetNode<LanDiscovery>("/root/LanDiscovery");

        _transferManager.OnTransferStarted += OnTransferStarted;
        _transferManager.OnTransferCompleted += OnTransferCompleted;
        _transferManager.OnTransferFailed += OnTransferFailed;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        _lanDiscovery.OnHostDiscovered += OnHostDiscovered;
        _transferManager.StartSendingAsMobileHost("1234");

        GD.Print("[PetReturnScene] Started as mobile host, waiting for PC to connect...");
    }

    private void OnHostDiscovered(string ip, int port, string pin, string petLocation)
    {
        // Mobile is host, PC should connect to us - we don't need to connect to PC
        // PC will discover us and connect
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Enviando pet de volta para o PC...";
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet enviado com sucesso!";
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.SetLocation(PetLocation.PC);
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SaveGame();
        _transferManager.StopTransfer();
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }

    private void OnTransferFailed(string reason)
    {
        _statusLabel.Text = "Falha na transferência: " + reason;
        _transferManager.StopTransfer();
        _isConnecting = false;
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }
}
