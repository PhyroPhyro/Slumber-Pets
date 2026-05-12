using Godot;
using System;

public partial class PetReturnScene : Control
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
        _authManager.OnSessionRestored += OnSessionRestored;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        _actionButton.Pressed += OnActionPressed;

        UpdateAuthUI();

        GD.Print("[PetReturnScene] Initialized");
    }

    private void OnSessionRestored(bool success)
    {
        UpdateAuthUI();
    }

    private void UpdateAuthUI()
    {
        if (_authManager.IsAuthenticated)
        {
            _statusLabel.Text = "Conectado como " + _authManager.UserEmail;
            _actionButton.Text = "Enviar Pet de Volta ao PC";
            _actionButton.Disabled = false;
        }
        else if (!_authManager.SessionRestoreComplete)
        {
            _statusLabel.Text = "Restaurando sessão...";
            _actionButton.Text = "Aguarde...";
            _actionButton.Disabled = true;
        }
        else
        {
            _statusLabel.Text = "Login necessário para retornar o pet";
            _actionButton.Text = "Fazer Login";
            _actionButton.Disabled = false;
        }
    }

    private void OnActionPressed()
    {
        if (_isProcessing) return;
        if (!_authManager.SessionRestoreComplete) return;

        if (!_authManager.IsAuthenticated)
        {
            SceneNavigator.ReturnScene = "res://scenes/main_mobile.tscn";
            GetTree().ChangeSceneToFile("res://scenes/ui/account_settings.tscn");
            return;
        }

        _isProcessing = true;
        _actionButton.Disabled = true;
        _statusLabel.Text = "Enviando pet de volta ao PC...";
        _transferManager.SendPetToPC();
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Enviando pet de volta para o PC...";
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet enviado com sucesso!";
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SaveGame();
        _transferManager.StopTransfer();
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
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
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }
}
