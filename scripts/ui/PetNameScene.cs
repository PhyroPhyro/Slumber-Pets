using Godot;
using System;

public partial class PetNameScene : Control
{
    private GameManager _gameManager;
    private SaveManager _saveManager;
    private LineEdit _nameEdit;
    private Button _confirmButton;
    private Label _errorLabel;
    private Label _petNameDisplay;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _saveManager = GetNode<SaveManager>("/root/SaveManager");

        _nameEdit = GetNode<LineEdit>("VBox/NameEdit");
        _confirmButton = GetNode<Button>("VBox/ConfirmButton");
        _errorLabel = GetNode<Label>("VBox/ErrorLabel");
        _petNameDisplay = GetNode<Label>("VBox/PetNameDisplay");

        _confirmButton.Pressed += OnConfirmPressed;
        _nameEdit.TextChanged += OnNameTextChanged;
        _nameEdit.TextSubmitted += OnTextSubmitted;

        _nameEdit.MaxLength = 16;
        _nameEdit.GrabFocus();

        _errorLabel.Visible = false;
        _petNameDisplay.Text = "";

        UpdateConfirmButtonState();

        GD.Print("[PetNameScene] Initialized");
    }

    private void OnNameTextChanged(string newText)
    {
        UpdateConfirmButtonState();
        UpdatePetNamePreview(newText);
    }

    private void OnTextSubmitted(string newText)
    {
        OnConfirmPressed();
    }

    private void UpdateConfirmButtonState()
    {
        string name = _nameEdit.Text.Trim();
        _confirmButton.Disabled = string.IsNullOrWhiteSpace(name) || name.Length > 16;
    }

    private void UpdatePetNamePreview(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _petNameDisplay.Text = "";
        }
        else
        {
            _petNameDisplay.Text = "\"" + name + "\"";
        }
    }

    private void OnConfirmPressed()
    {
        string name = _nameEdit.Text.Trim();

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

        GD.Print("[PetNameScene] Pet named: ", name);

        string targetScene = PlatformUtils.IsAndroidBuild()
            ? "res://scenes/main_mobile.tscn"
            : "res://scenes/main_desktop.tscn";

        GetTree().ChangeSceneToFile(targetScene);
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
}
