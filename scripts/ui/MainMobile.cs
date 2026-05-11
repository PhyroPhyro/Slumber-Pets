using Godot;
using System;

public partial class MainMobile : Control
{
    private Label _statusLabel;
    private Control _petAtHomePanel;
    private Control _petWithMePanel;
    private Button _sleepButton;
    private Button _walkButton;
    private Button _returnButton;
    private Button _receivePetButton;

    private GameManager _gameManager;
    private PetLocationManager _petLocationManager;

    public override void _Ready()
    {
        GD.Print("[MainMobile] Initializing...");
        GD.Print("[MainMobile] Platform: Android=", OS.HasFeature("Android"), " Windows=", OS.HasFeature("Windows"));

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");

        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _petAtHomePanel = GetNode<Control>("VBox/PetAtHome");
        _petWithMePanel = GetNode<Control>("VBox/PetWithMe");
        _receivePetButton = GetNode<Button>("VBox/PetAtHome/Container/ReceivePetButton");
        _sleepButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/SleepButton");
        _walkButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/WalkButton");
        _returnButton = GetNode<Button>("VBox/PetWithMe/Container/Buttons/ReturnButton");

        _sleepButton.Pressed += OnSleepPressed;
        _walkButton.Pressed += OnWalkPressed;
        _returnButton.Pressed += OnReturnPressed;
        _receivePetButton.Pressed += OnReceivePetPressed;

        _petLocationManager.OnLocationChanged += OnPetLocationChanged;

        GD.Print("[MainMobile] Pet location: ", _petLocationManager.CurrentLocation);
        UpdateUI();
    }

    private void UpdateUI()
    {
        PetLocation location = _petLocationManager.CurrentLocation;

        if (location == PetLocation.PC)
        {
            _petAtHomePanel.Visible = true;
            _petWithMePanel.Visible = false;
            _statusLabel.Text = "Seu pet está em casa (PC)";
            GD.Print("[MainMobile] Pet is at PC - showing 'Pet Away' screen");
        }
        else
        {
            _petAtHomePanel.Visible = false;
            _petWithMePanel.Visible = true;
            _statusLabel.Text = "Pet está com você!";
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
}
