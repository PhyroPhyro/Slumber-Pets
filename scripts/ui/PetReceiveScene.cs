using Godot;
using System;

public partial class PetReceiveScene : Control
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
        _lanDiscovery.StartListening();

        GD.Print("[PetReceiveScene] Listening for PC hosts...");
    }

    private void OnHostDiscovered(string ip, int port, string pin, string petLocation)
    {
        if (_isConnecting) return;

        if (petLocation == "pc")
        {
            _isConnecting = true;
            _statusLabel.Text = $"PC encontrado! Solicitando pet...";
            GD.Print("[PetReceiveScene] PC found: ", ip, ":", port, " - Requesting pet transfer");
            _transferManager.StartReceivingAsClient(ip, port);
        }
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Solicitando transferência do pet...";
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet recebido com sucesso!";
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.SetLocation(PetLocation.Mobile);
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SaveGame();
        _transferManager.StopTransfer();
        _lanDiscovery.StopListening();
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
        _lanDiscovery.StopListening();
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }
}
