using Godot;
using System;

public partial class PetSendScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private FirebaseAuthManager _authManager;
    private Button _actionButton;
    private bool _isProcessing = false;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _actionButton = GetNode<Button>("VBox/ActionButton");
        _transferManager = GetNode<PetTransferManager>("/root/PetTransferManager");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");

        _transferManager.OnTransferStarted += OnTransferStarted;
        _transferManager.OnTransferCompleted += OnTransferCompleted;
        _transferManager.OnTransferFailed += OnTransferFailed;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        _actionButton.Pressed += OnActionPressed;

        if (_authManager.IsLoggedIn)
        {
            _statusLabel.Text = "Conectado como " + _authManager.UserEmail;
            _actionButton.Text = "Enviar Pet para Mobile";
        }
        else
        {
            _statusLabel.Text = "Login necessário para enviar o pet";
            _actionButton.Text = "Fazer Login";
        }

        GD.Print("[PetSendScene] Initialized");
    }

    private void OnActionPressed()
    {
        if (_isProcessing) return;

        if (!_authManager.IsLoggedIn)
        {
            GetTree().ChangeSceneToFile("res://scenes/ui/account_settings.tscn");
            return;
        }

        _isProcessing = true;
        _actionButton.Disabled = true;
        _statusLabel.Text = "Enviando pet para o Firestore...";
        _transferManager.SendPetToMobile();
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
        _actionButton.Disabled = false;
        _isProcessing = false;
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }
}
