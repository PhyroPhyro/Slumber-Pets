using Godot;
using System;

public partial class ToolsMenuScene : Control
{
    private Button _renamePetButton;
    private Button _resetButton;
    private Button _backButton;
    private ConfirmationDialog _confirmDialog;
    private ConfirmationDialog _renameDialog;
    private LineEdit _renameEdit;
    private Label _statusLabel;
    private Label _errorLabel;

    private GameManager _gameManager;
    private SaveManager _saveManager;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _saveManager = GetNode<SaveManager>("/root/SaveManager");

        _renamePetButton = GetNode<Button>("VBox/RenamePetButton");
        _resetButton = GetNode<Button>("VBox/ResetButton");
        _backButton = GetNode<Button>("VBox/BackButton");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");

        _renamePetButton.Pressed += OnRenamePetPressed;
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

        _renameDialog = new ConfirmationDialog
        {
            Title = "Renomear Pet",
            DialogText = "Digite o novo nome do seu pet:",
            OkButtonText = "Confirmar",
            CancelButtonText = "Cancelar"
        };
        _renameDialog.Confirmed += OnRenameConfirmed;
        AddChild(_renameDialog);

        _renameEdit = new LineEdit { PlaceholderText = "Nome do pet", MaxLength = 16 };
        _renameEdit.TextSubmitted += OnRenameTextSubmitted;
        _renameDialog.AddChild(_renameEdit);

        _errorLabel = new Label { Visible = false };
        _renameDialog.AddChild(_errorLabel);
    }

    public override void _ExitTree()
    {
        if (_renameDialog != null)
        {
            _renameDialog.QueueFree();
        }
        if (_confirmDialog != null)
        {
            _confirmDialog.QueueFree();
        }
    }

    private void OnRenamePetPressed()
    {
        _errorLabel.Visible = false;
        _renameEdit.Text = _gameManager.CurrentSave.Pet.Name;
        _renameDialog.PopupCentered(new Vector2I(250, 120));
        _renameEdit.GrabFocus();
    }

    private void OnRenameTextSubmitted(string newText)
    {
        OnRenameConfirmed();
    }

    private void OnRenameConfirmed()
    {
        string name = _renameEdit.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Digite um nome para seu pet!");
            return;
        }

        if (name.Length > 16)
        {
            ShowError("Nome muito longo! Máximo 16 caracteres.");
            return;
        }

        name = SanitizeName(name);

        _gameManager.CurrentSave.Pet.Name = name;
        _gameManager.CurrentSave.IsPetNamed = true;
        _saveManager.SaveGame(_gameManager.CurrentSave);

        GD.Print("[ToolsMenuScene] Pet renamed: ", name);

        _statusLabel.Text = "Pet renomeado!";
        _statusLabel.Visible = true;
    }

    private string SanitizeName(string name)
    {
        char[] invalidChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*', '\0' };
        foreach (char c in invalidChars)
        {
            name = name.Replace(c.ToString(), "");
        }
        return name.Trim();
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
        _errorLabel.Modulate = new Color(1, 0.3f, 0.3f, 1);
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
