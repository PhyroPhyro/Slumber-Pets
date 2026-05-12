using Godot;
using System;

public partial class PetReceiveScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private FirebaseAuthManager _authManager;
    private FirestoreService _firestoreService;
    private Button _actionButton;
    private bool _isListening = false;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _actionButton = GetNode<Button>("VBox/ActionButton");
        _transferManager = GetNode<PetTransferManager>("/root/PetTransferManager");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        _firestoreService = GetNode<FirestoreService>("/root/FirestoreService");

        _transferManager.OnTransferStarted += OnTransferStarted;
        _transferManager.OnTransferCompleted += OnTransferCompleted;
        _transferManager.OnTransferFailed += OnTransferFailed;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        _actionButton.Pressed += OnActionPressed;

        if (_authManager.IsLoggedIn)
        {
            _statusLabel.Text = "Conectado como " + _authManager.UserEmail;
            _actionButton.Text = "Aguardando pet do PC";
        }
        else
        {
            _statusLabel.Text = "Login necessário para receber o pet";
            _actionButton.Text = "Fazer Login";
        }

        GD.Print("[PetReceiveScene] Initialized");
    }

    private void OnActionPressed()
    {
        if (_isListening) return;

        if (!_authManager.IsLoggedIn)
        {
            GetTree().ChangeSceneToFile("res://scenes/ui/account_settings.tscn");
            return;
        }

        _isListening = true;
        _actionButton.Disabled = true;
        _statusLabel.Text = "Aguardando pet do PC...";
        _transferManager.StartListeningForIncoming();
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Pet detectado! Recebendo...";
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet recebido com sucesso!";
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        petLocationManager.SetLocation(PetLocation.Mobile);
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
        _actionButton.Disabled = false;
        _isListening = false;
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }
}
