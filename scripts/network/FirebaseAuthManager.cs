using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class FirebaseAuthManager : Node
{
    [Signal] public delegate void OnLoginSuccessEventHandler();
    [Signal] public delegate void OnLoginFailedEventHandler(string reason);
    [Signal] public delegate void OnLogoutEventHandler();
    [Signal] public delegate void OnTokenRefreshedEventHandler();
    [Signal] public delegate void OnSessionRestoredEventHandler(bool success);

    private const string TokenSaveKey = "user://saves/firebase_token.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private System.Net.Http.HttpClient _httpClient;
    private string _idToken = "";
    private string _refreshToken = "";
    private string _localId = "";
    private string _email = "";
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public bool IsLoggedIn => !string.IsNullOrEmpty(_idToken) && DateTime.UtcNow < _tokenExpiresAt;
    public bool HasSavedSession => !string.IsNullOrEmpty(_refreshToken);
    public string UserId => _localId;
    public string UserEmail => _email;
    public string IdToken => _idToken;

    public bool SessionRestoreComplete { get; private set; } = false;
    public bool SessionRestoreSuccess { get; private set; } = false;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_idToken);

    public override void _Ready()
    {
        _httpClient = new System.Net.Http.HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        FirebaseConfig.Load();
        LoadSavedToken();
        GD.Print("[FirebaseAuthManager] Initialized");
    }

    public override void _ExitTree()
    {
        _httpClient?.Dispose();
    }

    public async void LoginWithEmail(string email, string password)
    {
        if (!FirebaseConfig.IsConfigured)
        {
            Callable.From(() => EmitSignal(SignalName.OnLoginFailed, "Firebase not configured")).CallDeferred();
            return;
        }

        try
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseConfig.ApiKey}";
            var payload = new { email = email, password = password, returnSecureToken = true };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AuthResponse>(responseBody, JsonOptions);
                if (result != null)
                {
                    SetTokens(result);
                    SaveToken();
                    GD.Print("[FirebaseAuthManager] Login successful: ", email, " | Token length: ", result.IdToken?.Length ?? 0);
                    Callable.From(() => EmitSignal(SignalName.OnLoginSuccess)).CallDeferred();
                }
            }
            else
            {
                var error = JsonSerializer.Deserialize<AuthError>(responseBody, JsonOptions);
                string message = error?.Error?.Message ?? "Unknown error";
                GD.PrintErr("[FirebaseAuthManager] Login failed: ", message);
                Callable.From(() => EmitSignal(SignalName.OnLoginFailed, TranslateFirebaseError(message))).CallDeferred();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseAuthManager] Login error: ", e.Message);
            Callable.From(() => EmitSignal(SignalName.OnLoginFailed, "Connection error")).CallDeferred();
        }
    }

    public async void RegisterWithEmail(string email, string password)
    {
        if (!FirebaseConfig.IsConfigured)
        {
            Callable.From(() => EmitSignal(SignalName.OnLoginFailed, "Firebase not configured")).CallDeferred();
            return;
        }

        try
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseConfig.ApiKey}";
            var payload = new { email = email, password = password, returnSecureToken = true };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AuthResponse>(responseBody, JsonOptions);
                if (result != null)
                {
                    SetTokens(result);
                    SaveToken();
                    GD.Print("[FirebaseAuthManager] Registration successful: ", email);
                    Callable.From(() => EmitSignal(SignalName.OnLoginSuccess)).CallDeferred();
                }
            }
            else
            {
                var error = JsonSerializer.Deserialize<AuthError>(responseBody, JsonOptions);
                string message = error?.Error?.Message ?? "Unknown error";
                GD.PrintErr("[FirebaseAuthManager] Registration failed: ", message);
                Callable.From(() => EmitSignal(SignalName.OnLoginFailed, TranslateFirebaseError(message))).CallDeferred();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseAuthManager] Registration error: ", e.Message);
            Callable.From(() => EmitSignal(SignalName.OnLoginFailed, "Connection error")).CallDeferred();
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            GD.PrintErr("[FirebaseAuthManager] RefreshTokenAsync: No refresh token available");
            return false;
        }

        GD.Print("[FirebaseAuthManager] RefreshTokenAsync: attempting refresh with token length=", _refreshToken.Length);

        try
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={FirebaseConfig.ApiKey}";
            var payload = new { grant_type = "refresh_token", refresh_token = _refreshToken };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            GD.Print("[FirebaseAuthManager] RefreshTokenAsync: response status=", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TokenRefreshResponse>(responseBody, JsonOptions);
                if (result != null)
                {
                    GD.Print("[FirebaseAuthManager] RefreshTokenAsync: deserialized IdToken len=", result.IdToken?.Length ?? 0, " ExpiresIn=", result.ExpiresIn);
                    
                    _idToken = result.IdToken;
                    long.TryParse(result.ExpiresIn, out long expiresIn);
                    _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300);
                    SaveToken();
                    GD.Print("[FirebaseAuthManager] RefreshTokenAsync: Token refreshed successfully, expires at=", _tokenExpiresAt, " IsAuthenticated=", IsAuthenticated);
                    Callable.From(() => EmitSignal(SignalName.OnTokenRefreshed)).CallDeferred();
                    return true;
                }
            }
            else
            {
                GD.PrintErr("[FirebaseAuthManager] RefreshTokenAsync: Token refresh failed, response=", responseBody.Substring(0, Math.Min(200, responseBody.Length)));
                Logout();
                return false;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseAuthManager] RefreshTokenAsync: Token refresh error: ", e.Message);
        }

        return false;
    }

    public void Logout()
    {
        _idToken = "";
        _refreshToken = "";
        _localId = "";
        _email = "";
        _tokenExpiresAt = DateTime.MinValue;
        SessionRestoreComplete = true;
        SessionRestoreSuccess = false;
        DeleteSavedToken();
        GD.Print("[FirebaseAuthManager] Logged out");
        EmitSignal(SignalName.OnLogout);
    }

    public async Task<string> GetValidTokenAsync()
    {
        if (IsLoggedIn)
            return _idToken;

        if (!string.IsNullOrEmpty(_refreshToken))
        {
            bool success = await RefreshTokenAsync();
            if (success && IsLoggedIn)
                return _idToken;
        }

        return null;
    }

    private void SetTokens(AuthResponse result)
    {
        _idToken = result.IdToken;
        _refreshToken = result.RefreshToken;
        _localId = result.LocalId;
        _email = result.Email;
        long.TryParse(result.ExpiresIn, out long expiresIn);
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 300);
    }

    private void SaveToken()
    {
        try
        {
            var data = new SavedTokenData
            {
                IdToken = _idToken,
                RefreshToken = _refreshToken,
                LocalId = _localId,
                Email = _email,
                ExpiresAt = _tokenExpiresAt.ToString("o")
            };
            string json = JsonSerializer.Serialize(data);
            GD.Print("[FirebaseAuthManager] Saving token: RefreshToken length=", _refreshToken?.Length ?? 0, " ExpiresAt=", _tokenExpiresAt);
            
            string dir = TokenSaveKey.Substring(0, TokenSaveKey.LastIndexOf('/'));
            if (!DirAccess.DirExistsAbsolute(dir))
            {
                DirAccess.MakeDirAbsolute(dir);
            }
            
            using FileAccess file = FileAccess.Open(TokenSaveKey, FileAccess.ModeFlags.Write);
            file.StoreString(json);
            GD.Print("[FirebaseAuthManager] Token saved to ", TokenSaveKey);
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseAuthManager] Error saving token: ", e.Message);
        }
    }

    private void LoadSavedToken()
    {
        SessionRestoreComplete = false;
        SessionRestoreSuccess = false;

        try
        {
            GD.Print("[FirebaseAuthManager] LoadSavedToken: checking ", TokenSaveKey);
            
            if (!FileAccess.FileExists(TokenSaveKey))
            {
                GD.Print("[FirebaseAuthManager] LoadSavedToken: file does not exist");
                SessionRestoreComplete = true;
                Callable.From(() => EmitSignal(SignalName.OnSessionRestored, false)).CallDeferred();
                return;
            }

            using FileAccess file = FileAccess.Open(TokenSaveKey, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            GD.Print("[FirebaseAuthManager] LoadSavedToken: file content length=", json?.Length ?? 0);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                GD.Print("[FirebaseAuthManager] LoadSavedToken: file is empty");
                DeleteSavedToken();
                SessionRestoreComplete = true;
                Callable.From(() => EmitSignal(SignalName.OnSessionRestored, false)).CallDeferred();
                return;
            }

            var data = JsonSerializer.Deserialize<SavedTokenData>(json);
            if (data == null || string.IsNullOrEmpty(data.RefreshToken) || !DateTime.TryParse(data.ExpiresAt, out DateTime expiresAt))
            {
                GD.Print("[FirebaseAuthManager] LoadSavedToken: invalid data - RefreshToken=", string.IsNullOrEmpty(data?.RefreshToken), " ExpiresAt=", data?.ExpiresAt);
                DeleteSavedToken();
                SessionRestoreComplete = true;
                Callable.From(() => EmitSignal(SignalName.OnSessionRestored, false)).CallDeferred();
                return;
            }

            _idToken = data.IdToken;
            _refreshToken = data.RefreshToken;
            _localId = data.LocalId;
            _email = data.Email;
            _tokenExpiresAt = expiresAt;

            GD.Print("[FirebaseAuthManager] LoadSavedToken: loaded - Email=", _email, " ExpiresAt=", _tokenExpiresAt, " Now=", DateTime.UtcNow, " IsLoggedIn=", IsLoggedIn);

            if (DateTime.UtcNow >= _tokenExpiresAt)
            {
                GD.Print("[FirebaseAuthManager] Token expired, restoring session...");
                RestoreSessionAsync();
            }
            else if (IsLoggedIn)
            {
                GD.Print("[FirebaseAuthManager] Restored session for: ", _email);
                SessionRestoreComplete = true;
                SessionRestoreSuccess = true;
                Callable.From(() => EmitSignal(SignalName.OnSessionRestored, true)).CallDeferred();
            }
            else
            {
                GD.Print("[FirebaseAuthManager] Token data invalid after load");
                DeleteSavedToken();
                SessionRestoreComplete = true;
                Callable.From(() => EmitSignal(SignalName.OnSessionRestored, false)).CallDeferred();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseAuthManager] Error loading saved token: ", e.Message);
            DeleteSavedToken();
            SessionRestoreComplete = true;
            Callable.From(() => EmitSignal(SignalName.OnSessionRestored, false)).CallDeferred();
        }
    }

    private async void RestoreSessionAsync()
    {
        GD.Print("[FirebaseAuthManager] RestoreSessionAsync: starting refresh");
        bool success = await RefreshTokenAsync();
        GD.Print("[FirebaseAuthManager] RestoreSessionAsync: refresh result=", success, " IsAuthenticated=", IsAuthenticated);
        SessionRestoreComplete = true;
        SessionRestoreSuccess = success;
        Callable.From(() => EmitSignal(SignalName.OnSessionRestored, success)).CallDeferred();
    }

    private void DeleteSavedToken()
    {
        try
        {
            if (FileAccess.FileExists(TokenSaveKey))
                DirAccess.RemoveAbsolute(TokenSaveKey);
        }
        catch { }
    }

    private string TranslateFirebaseError(string code)
    {
        return code switch
        {
            "EMAIL_NOT_FOUND" => "Email not found",
            "INVALID_PASSWORD" => "Incorrect password",
            "EMAIL_EXISTS" => "Email already in use",
            "WEAK_PASSWORD" => "Password must be at least 6 characters",
            "INVALID_LOGIN_CREDENTIALS" => "Invalid email or password",
            "USER_DISABLED" => "Account disabled",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts, try later",
            _ => code
        };
    }
}

public class AuthResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("idToken")]
    public string IdToken { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("localId")]
    public string LocalId { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; }
}

public class AuthError
{
    public AuthErrorDetail Error { get; set; }
}

public class AuthErrorDetail
{
    public int Code { get; set; }
    public string Message { get; set; }
}

public class TokenRefreshResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id_token")]
    public string IdToken { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
}

public class SavedTokenData
{
    public string IdToken { get; set; }
    public string RefreshToken { get; set; }
    public string LocalId { get; set; }
    public string Email { get; set; }
    public string ExpiresAt { get; set; }
}
