using Godot;
using System;

public partial class PetReceiveOnDesktopScene : Control
{
    private Label _statusLabel;
    private PetTransferManager _transferManager;
    private FirebaseAuthManager _authManager;
    private Button _actionButton;
    private bool _isListening = false;
    private ProgressBar _progressBar;
    private TextureRect _loadingIcon;
    private float _loadingSpinAngle = 0f;

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

        _progressBar = GetNodeOrNull<ProgressBar>("VBox/ProgressBar");
        _loadingIcon = GetNodeOrNull<TextureRect>("VBox/LoadingIcon");

        if (_progressBar != null) _progressBar.Visible = false;
        if (_loadingIcon != null) _loadingIcon.Visible = false;

        UpdateAuthUI();

        GD.Print("[PetReceiveOnDesktopScene] Initialized");
    }

    public override void _Process(double delta)
    {
        if (_loadingIcon != null && _loadingIcon.Visible)
        {
            _loadingSpinAngle += (float)(delta * 180.0);
            _loadingIcon.Rotation = Mathf.DegToRad(_loadingSpinAngle);
        }
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
            _actionButton.Text = "Aguardando pet do Mobile";
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
            _statusLabel.Text = "Login necessário para receber o pet";
            _actionButton.Text = "Fazer Login";
            _actionButton.Disabled = false;
        }
    }

    private void OnActionPressed()
    {
        if (_isListening) return;
        if (!_authManager.SessionRestoreComplete) return;

        if (!_authManager.IsAuthenticated)
        {
            SceneNavigator.ReturnScene = "res://scenes/main_desktop.tscn";
            GetTree().ChangeSceneToFile("res://scenes/ui/account_settings.tscn");
            return;
        }

        _isListening = true;
        _actionButton.Disabled = true;
        _statusLabel.Text = "Aguardando pet do mobile...";
        if (_progressBar != null)
        {
            _progressBar.Visible = true;
            _progressBar.Value = 0;
        }
        if (_loadingIcon != null) _loadingIcon.Visible = true;
        _transferManager.StartListeningForIncoming();
    }

    private void OnTransferStarted()
    {
        _statusLabel.Text = "Pet detectado! Recebendo...";
    }

    private void OnTransferProgress(string status, float progress)
    {
        _statusLabel.Text = status;
        if (_progressBar != null)
        {
            _progressBar.Visible = true;
            _progressBar.Value = progress * 100;
        }
    }

    private async void OnTransferCompleted()
    {
        _statusLabel.Text = "Pet recebido com sucesso!";
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        if (_progressBar != null)
        {
            _progressBar.Value = 100;
        }
        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SaveGame();
        _transferManager.StopTransfer();
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }

    private void OnTransferFailed(string reason)
    {
        _statusLabel.Text = "Falha na transferência: " + reason;
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        if (_progressBar != null) _progressBar.Visible = false;
        _transferManager.StopTransfer();
        _actionButton.Disabled = false;
        _isListening = false;
    }

    private void OnCancelPressed()
    {
        _transferManager.StopTransfer();
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        if (_progressBar != null) _progressBar.Visible = false;
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }
}
