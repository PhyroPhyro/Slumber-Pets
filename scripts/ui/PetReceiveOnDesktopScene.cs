using Godot;
using System;

public partial class PetReceiveOnDesktopScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private LanDiscovery _lanDiscovery;
    private bool _isConnecting = false;
    private Timer _searchTimeoutTimer;
    private const float SearchTimeoutSeconds = 30.0f;

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

        _searchTimeoutTimer = new Timer();
        _searchTimeoutTimer.WaitTime = SearchTimeoutSeconds;
        _searchTimeoutTimer.OneShot = true;
        _searchTimeoutTimer.Timeout += OnSearchTimeout;
        AddChild(_searchTimeoutTimer);
        _searchTimeoutTimer.Start();

        GD.Print("[PetReceiveOnDesktopScene] Listening for mobile hosts... (timeout: ", SearchTimeoutSeconds, "s)");
    }

    private void OnHostDiscovered(string ip, int port, string pin, string petLocation)
    {
        if (_isConnecting) return;

        if (petLocation == "mobile")
        {
            _isConnecting = true;
            _searchTimeoutTimer.Stop();
            _statusLabel.Text = "Mobile encontrado! Conectando para receber pet...";
            GD.Print("[PetReceiveOnDesktopScene] Mobile found: ", ip, ":", port, " - Connecting to receive pet");
            _transferManager.StartReceivingFromMobile(ip, port);
        }
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Recebendo pet do mobile...";
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet recebido com sucesso!";
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.SetLocation(PetLocation.PC);
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SaveGame();
        _transferManager.StopTransfer();
        _lanDiscovery.StopListening();
        await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }

    private void OnTransferFailed(string reason)
    {
        _statusLabel.Text = "Falha na transferência: " + reason;
        _transferManager.StopTransfer();
        _lanDiscovery.StopListening();
        _isConnecting = false;
    }

    private void OnSearchTimeout()
    {
        if (!_isConnecting)
        {
            _statusLabel.Text = "Tempo esgotado - Mobile não encontrado. Verifique se ambos estão na mesma rede.";
            GD.PrintErr("[PetReceiveOnDesktopScene] Search timeout - no mobile found");
            _transferManager.StopTransfer();
            _lanDiscovery.StopListening();
        }
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        _lanDiscovery.StopListening();
        _searchTimeoutTimer.Stop();
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }
}
