using Godot;
using System;

public partial class ToolsMenuScene : Control
{
    private Button _resetButton;
    private Button _backButton;
    private ConfirmationDialog _confirmDialog;
    private Label _statusLabel;

    public override void _Ready()
    {
        _resetButton = GetNode<Button>("VBox/ResetButton");
        _backButton = GetNode<Button>("VBox/BackButton");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");

        _resetButton.Pressed += OnResetPressed;
        _backButton.Pressed += OnBackPressed;

        _confirmDialog = new ConfirmationDialog
        {
            Title = "Confirmar Reset",
            DialogText = "Tem certeza que deseja apagar todo o progresso? Esta ação não pode ser desfeita.",
            OkButtonText = "Apagar Tudo",
            CancelButtonText = "Cancelar"
        };
        _confirmDialog.Confirmed += OnResetConfirmed;
        AddChild(_confirmDialog);
    }

    private void OnResetPressed()
    {
        _confirmDialog.PopupCentered(new Vector2I(250, 100));
    }

    private async void OnResetConfirmed()
    {
        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        authManager.Logout();

        var saveManager = GetNode<SaveManager>("/root/SaveManager");
        saveManager.ResetSave();

        _statusLabel.Text = "Save limpo! Reiniciando...";
        _statusLabel.Visible = true;

        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        GetTree().ChangeSceneToFile("res://scenes/scene_loader.tscn");
    }

    private void OnBackPressed()
    {
        var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
        string scene = petLocationManager.CurrentLocation == PetLocation.Mobile
            ? "res://scenes/main_mobile.tscn"
            : "res://scenes/main_desktop.tscn";
        GetTree().ChangeSceneToFile(scene);
    }
}
