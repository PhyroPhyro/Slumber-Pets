using Godot;
using System;

public partial class MainMobile : Control
{
    private Label _statusLabel;
    private Label _firebaseStatusLabel;
    private Control _petAtHomePanel;
    private Control _petWithMePanel;
    private Button _sleepButton;
    private Button _walkButton;
    private Button _returnButton;
    private Button _receivePetButton;
    private Button _toolsButton;

    private GameManager _gameManager;
    private PetLocationManager _petLocationManager;
    private FirebaseAuthManager _authManager;

    public override void _Ready()
    {
        GD.Print("[MainMobile] Initializing...");
        GD.Print("[MainMobile] Platform: Android=", OS.HasFeature("Android"), " Windows=", OS.HasFeature("Windows"));

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");

        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _firebaseStatusLabel = GetNode<Label>("VBox/FirebaseStatusLabel");
        _petAtHomePanel = GetNode<Control>("VBox/PetAtHome");
        _petWithMePanel = GetNode<Control>("VBox/PetWithMe");
        _receivePetButton = GetNode<Button>("VBox/PetAtHome/Container/ReceivePetButton");
        _sleepButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/SleepButton");
        _walkButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/WalkButton");
        _returnButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/ReturnButton");
        _toolsButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/ToolsButton");

        _sleepButton.Pressed += OnSleepPressed;
        _walkButton.Pressed += OnWalkPressed;
        _returnButton.Pressed += OnReturnPressed;
        _receivePetButton.Pressed += OnReceivePetPressed;
        _toolsButton.Pressed += OnToolsPressed;

        _petLocationManager.OnLocationChanged += OnPetLocationChanged;
        _authManager.OnSessionRestored += OnSessionRestored;
        _authManager.OnLoginSuccess += OnAuthChanged;
        _authManager.OnLogout += OnAuthChanged;
        _authManager.OnTokenRefreshed += OnAuthChanged;

        GD.Print("[MainMobile] Pet location: ", _petLocationManager.CurrentLocation);
        UpdateUI();
        UpdateFirebaseStatus();
    }

    private void UpdateUI()
    {
        PetLocation location = _petLocationManager.CurrentLocation;
        string petName = _gameManager.GetPetState().Name;

        if (location == PetLocation.PC)
        {
            _petAtHomePanel.Visible = true;
            _petWithMePanel.Visible = false;
            _statusLabel.Text = petName + " está em casa (PC)";
            GD.Print("[MainMobile] Pet is at PC - showing 'Pet Away' screen");
        }
        else
        {
            _petAtHomePanel.Visible = false;
            _petWithMePanel.Visible = true;
            _statusLabel.Text = petName + " está com você!";
            GD.Print("[MainMobile] Pet is on mobile - showing action buttons");
        }
    }

    private void OnPetLocationChanged(PetLocation newLocation)
    {
        CallDeferred(MethodName.UpdateUI);
    }

    private void OnSleepPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/sleep/sleep_mode.tscn");
    }

    private void OnWalkPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/walk/walk_mode.tscn");
    }

    private void OnReturnPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/transfer/pet_return.tscn");
    }

    private void OnReceivePetPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/transfer/pet_receive.tscn");
    }

    private void OnToolsPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/ui/tools_menu.tscn");
    }

    private void UpdateFirebaseStatus()
    {
        if (!_authManager.SessionRestoreComplete)
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(1, 1, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Restaurando sessão...";
        }
        else if (_authManager.IsAuthenticated)
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(0, 1, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Conectado (" + _authManager.UserEmail + ")";
        }
        else
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(1, 0, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Desconectado";
        }
    }

    private void OnSessionRestored(bool success)
    {
        CallDeferred(MethodName.UpdateFirebaseStatus);
    }

    private void OnAuthChanged()
    {
        CallDeferred(MethodName.UpdateFirebaseStatus);
    }
}
