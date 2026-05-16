using Godot;
using System;

public partial class PetTravelingScene : Control
{
    private Label _statusLabel;
    private Label _detailLabel;
    private TextureRect _loadingIcon;
    private float _loadingSpinAngle = 0f;
    private PetLocationManager _petLocationManager;
    private Timer _timeoutTimer;
    private const float TimeoutSeconds = 60f;
    private bool _isWaiting = false;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _detailLabel = GetNode<Label>("VBox/DetailLabel");
        _loadingIcon = GetNodeOrNull<TextureRect>("VBox/LoadingIcon");
        _petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");

        _petLocationManager.OnPetSent += OnPetSent;
        _petLocationManager.OnPetReceived += OnPetReceived;
        _petLocationManager.OnTransferFailed += OnTransferFailed;
        _petLocationManager.OnLocationChanged += OnLocationChanged;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        _timeoutTimer = new Timer();
        _timeoutTimer.WaitTime = TimeoutSeconds;
        _timeoutTimer.OneShot = true;
        _timeoutTimer.Timeout += OnTimeout;
        AddChild(_timeoutTimer);

        UpdateStatus();
        _timeoutTimer.Start();
        _isWaiting = true;

        GD.Print("[PetTravelingScene] Initialized, waiting for transfer to complete");
    }

    public override void _Process(double delta)
    {
        if (_loadingIcon != null && _loadingIcon.Visible)
        {
            _loadingSpinAngle += (float)(delta * 180.0);
            _loadingIcon.Rotation = Mathf.DegToRad(_loadingSpinAngle);
        }
    }

    private void UpdateStatus()
    {
        if (_petLocationManager.TransferState == PetTransferState.Sending)
        {
            _statusLabel.Text = "Pet em Trânsito...";
            _detailLabel.Text = "Enviando dados para a nuvem";
            if (_loadingIcon != null) _loadingIcon.Visible = true;
        }
        else if (_petLocationManager.TransferState == PetTransferState.Receiving)
        {
            _statusLabel.Text = "Pet em Trânsito...";
            _detailLabel.Text = "Recebendo dados da nuvem";
            if (_loadingIcon != null) _loadingIcon.Visible = true;
        }
        else if (_petLocationManager.TransferState == PetTransferState.Failed)
        {
            _statusLabel.Text = "Transferência Falhou";
            _detailLabel.Text = "Pet permaneceu no dispositivo atual";
            if (_loadingIcon != null) _loadingIcon.Visible = false;
        }
        else
        {
            _statusLabel.Text = "Aguardando transferência...";
            _detailLabel.Text = "Nenhuma transferência em andamento";
            if (_loadingIcon != null) _loadingIcon.Visible = false;
        }
    }

    private void OnPetSent()
    {
        _statusLabel.Text = "Pet Enviado!";
        _detailLabel.Text = "Transferência concluída com sucesso";
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        _isWaiting = false;
        _timeoutTimer.Stop();
        CallDeferred(nameof(DelayedReturn));
    }

    private void OnPetReceived()
    {
        _statusLabel.Text = "Pet Recebido!";
        _detailLabel.Text = "Transferência concluída com sucesso";
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        _isWaiting = false;
        _timeoutTimer.Stop();
        CallDeferred(nameof(DelayedReturn));
    }

    private void OnTransferFailed(string reason)
    {
        _statusLabel.Text = "Falha na Transferência";
        _detailLabel.Text = reason;
        if (_loadingIcon != null) _loadingIcon.Visible = false;
        _isWaiting = false;
        _timeoutTimer.Stop();
    }

    private void OnLocationChanged(PetLocation newLocation)
    {
        if (_isWaiting)
        {
            CallDeferred(nameof(DelayedReturn));
        }
    }

    private void OnTimeout()
    {
        if (_isWaiting)
        {
            _statusLabel.Text = "Tempo Esgotado";
            _detailLabel.Text = "Transferência demorou muito. Verifique sua conexão.";
            if (_loadingIcon != null) _loadingIcon.Visible = false;
            _isWaiting = false;
        }
    }

    private async void DelayedReturn()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        var gameManager = GetNode<GameManager>("/root/GameManager");
        if (_petLocationManager.CurrentLocation == PetLocation.PC)
        {
            GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
        }
    }

    private void OnCancelPressed()
    {
        _timeoutTimer.Stop();
        _isWaiting = false;
        var gameManager = GetNode<GameManager>("/root/GameManager");
        if (_petLocationManager.CurrentLocation == PetLocation.PC)
        {
            GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
        }
        else
        {
            GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
        }
    }
}
