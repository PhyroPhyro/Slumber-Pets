using Godot;
using System;

public partial class AccountSettingsScene : Control
{
    private Label _statusLabel;
    private Label _emailLabel;
    private LineEdit _emailEdit;
    private LineEdit _passwordEdit;
    private Button _loginButton;
    private Button _registerButton;
    private Button _logoutButton;
    private FirebaseAuthManager _authManager;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        _emailLabel = GetNode<Label>("VBox/EmailLabel");
        _emailEdit = GetNode<LineEdit>("VBox/EmailEdit");
        _passwordEdit = GetNode<LineEdit>("VBox/PasswordEdit");
        _loginButton = GetNode<Button>("VBox/LoginButton");
        _registerButton = GetNode<Button>("VBox/RegisterButton");
        _logoutButton = GetNode<Button>("VBox/LogoutButton");
        _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");

        _authManager.OnLoginSuccess += OnLoginSuccess;
        _authManager.OnLoginFailed += OnLoginFailed;
        _authManager.OnLogout += OnLogout;

        _loginButton.Pressed += OnLoginPressed;
        _registerButton.Pressed += OnRegisterPressed;
        _logoutButton.Pressed += OnLogoutPressed;

        var cancelButton = GetNode<Button>("VBox/CancelButton");
        cancelButton.Pressed += OnCancelPressed;

        UpdateUI();

        GD.Print("[AccountSettingsScene] Initialized");
    }

    private void UpdateUI()
    {
        if (_authManager.IsLoggedIn)
        {
            _statusLabel.Text = "Conectado";
            _emailLabel.Text = _authManager.UserEmail;
            _emailEdit.Visible = false;
            _passwordEdit.Visible = false;
            _loginButton.Visible = false;
            _registerButton.Visible = false;
            _logoutButton.Visible = true;
        }
        else
        {
            _statusLabel.Text = "Não conectado";
            _emailLabel.Text = "";
            _emailEdit.Visible = true;
            _passwordEdit.Visible = true;
            _loginButton.Visible = true;
            _registerButton.Visible = true;
            _logoutButton.Visible = false;
        }
    }

    private void OnLoginPressed()
    {
        string email = _emailEdit.Text.Trim();
        string password = _passwordEdit.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            _statusLabel.Text = "Preencha email e senha";
            return;
        }

        _statusLabel.Text = "Entrando...";
        _loginButton.Disabled = true;
        _registerButton.Disabled = true;
        _authManager.LoginWithEmail(email, password);
    }

    private void OnRegisterPressed()
    {
        string email = _emailEdit.Text.Trim();
        string password = _passwordEdit.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            _statusLabel.Text = "Preencha email e senha";
            return;
        }

        if (password.Length < 6)
        {
            _statusLabel.Text = "Senha deve ter pelo menos 6 caracteres";
            return;
        }

        _statusLabel.Text = "Criando conta...";
        _loginButton.Disabled = true;
        _registerButton.Disabled = true;
        _authManager.RegisterWithEmail(email, password);
    }

    private void OnLogoutPressed()
    {
        _authManager.Logout();
    }

    private void OnLoginSuccess()
    {
        GD.Print("[AccountSettingsScene] OnLoginSuccess signal received!");
        _statusLabel.Text = "Login bem-sucedido!";
        UpdateUI();
        _loginButton.Disabled = false;
        _registerButton.Disabled = false;
    }

    private void OnLoginFailed(string reason)
    {
        _statusLabel.Text = "Erro: " + reason;
        _loginButton.Disabled = false;
        _registerButton.Disabled = false;
    }

    private void OnLogout()
    {
        _statusLabel.Text = "Desconectado";
        UpdateUI();
    }

    private void OnCancelPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
    }
}
