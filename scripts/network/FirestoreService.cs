using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class FirestoreService : Node
{
    [Signal] public delegate void OnSnapshotUpdatedEventHandler();
    [Signal] public delegate void OnSnapshotWriteCompleteEventHandler();
    [Signal] public delegate void OnNetworkErrorEventHandler(string reason);
    [Signal] public delegate void OnConnectionStatusChangedEventHandler(bool isConnected);

    private System.Net.Http.HttpClient _httpClient;
    private Timer _pollTimer;
    private string _lastKnownTransferId = "";
    private bool _isListening = false;
    private bool _isConnected = false;
    private const float PollIntervalMinSeconds = 5.0f;
    private const float PollIntervalMaxSeconds = 60.0f;
    private const float PollBackoffMultiplier = 1.5f;
    private const float RateLimitRetryDelaySeconds = 30.0f;
    private float _currentPollInterval;
    private int _consecutiveFailures;
    private int _consecutiveSuccesses;
    private const int SuccessesToResetBackoff = 3;
    private bool _isRateLimited;
    private int _totalPolls;
    private int _totalPollSuccesses;
    private int _totalPollFailures;
    private DateTime _pollingStartTime;

    public PetSnapshot LastReceivedSnapshot { get; private set; }
    public bool IsListening => _isListening;

    public void SetLastKnownTransferId(string transferId)
    {
        _lastKnownTransferId = transferId;
    }

    public override void _Ready()
    {
        _httpClient = new System.Net.Http.HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);

        _currentPollInterval = PollIntervalMinSeconds;
        _consecutiveFailures = 0;
        _consecutiveSuccesses = 0;

        _pollTimer = new Timer();
        _pollTimer.WaitTime = _currentPollInterval;
        _pollTimer.OneShot = true;
        _pollTimer.Timeout += OnPollTick;
        AddChild(_pollTimer);

        FirebaseConfig.Load();
        GD.Print("[FirestoreService] Initialized (polling: ", PollIntervalMinSeconds, "s-", PollIntervalMaxSeconds, "s, backoff x", PollBackoffMultiplier, ")");
    }

    public override void _ExitTree()
    {
        StopListening();
        _httpClient?.Dispose();
    }

    public async void SetPetSnapshot(PetSnapshot snapshot)
    {
        if (!FirebaseConfig.IsConfigured)
        {
            EmitSignal(SignalName.OnNetworkError, "Firebase not configured");
            return;
        }

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            EmitSignal(SignalName.OnNetworkError, "Not authenticated");
            return;
        }

        try
        {
            string userId = authManager.UserId;
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}";

            var doc = new FirestoreDocument
            {
                Fields = new FirestoreFields
                {
                    { "Version", new FirestoreValue { IntegerValue = snapshot.Version.ToString() } },
                    { "TransferId", new FirestoreValue { StringValue = snapshot.TransferId } },
                    { "PetLocation", new FirestoreValue { StringValue = snapshot.PetLocation } },
                    { "Status", new FirestoreValue { StringValue = snapshot.Status } },
                    { "TransferredAt", new FirestoreValue { StringValue = snapshot.TransferredAt.ToString("o") } },
                    { "ConfirmedBy", new FirestoreValue { StringValue = "" } }
                }
            };

            doc.Fields["Pet"] = new FirestoreValue
            {
                MapValue = new FirestoreMap
                {
                    Fields = new FirestoreFields
                    {
                        { "Hunger", new FirestoreValue { DoubleValue = snapshot.Pet.Hunger } },
                        { "Happiness", new FirestoreValue { DoubleValue = snapshot.Pet.Happiness } },
                        { "Energy", new FirestoreValue { DoubleValue = snapshot.Pet.Energy } },
                        { "LastUpdatedAt", new FirestoreValue { StringValue = snapshot.Pet.LastUpdatedAt.ToString("o") } }
                    }
                }
            };

            doc.Fields["Inventory"] = new FirestoreValue
            {
                MapValue = new FirestoreMap
                {
                    Fields = new FirestoreFields
                    {
                        { "StarCoins", new FirestoreValue { IntegerValue = snapshot.Inventory.StarCoins.ToString() } }
                    }
                }
            };

            doc.Fields["AccumulatedReports"] = new FirestoreValue
            {
                MapValue = new FirestoreMap
                {
                    Fields = new FirestoreFields
                    {
                        { "SleepReports", new FirestoreValue { ArrayValue = new FirestoreArray { Values = new System.Collections.Generic.List<FirestoreValue>() } } },
                        { "WalkReports", new FirestoreValue { ArrayValue = new FirestoreArray { Values = new System.Collections.Generic.List<FirestoreValue>() } } }
                    }
                }
            };

            string json = JsonSerializer.Serialize(doc);
            GD.Print("[FirestoreService] Sending JSON: ", json.Substring(0, Math.Min(500, json.Length)));
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _lastKnownTransferId = snapshot.TransferId;
                GD.Print("[FirestoreService] PetSnapshot written: transferId=", snapshot.TransferId, " location=", snapshot.PetLocation);
                EmitSignal(SignalName.OnSnapshotWriteComplete);
                SetConnectionStatus(true);
                ResetBackoff();
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                if ((int)response.StatusCode == 429)
                {
                    GD.PrintErr("[FirestoreService] Rate limited on write (429), backing off for ", RateLimitRetryDelaySeconds, "s");
                    _isRateLimited = true;
                    _pollTimer.Stop();
                    _pollTimer.WaitTime = RateLimitRetryDelaySeconds;
                    if (_isListening) _pollTimer.Start();
                }
                GD.PrintErr("[FirestoreService] Write failed: ", response.StatusCode, " ", body);
                EmitSignal(SignalName.OnNetworkError, "Failed to write snapshot");
                SetConnectionStatus(false);
                RecordFailure();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Write error: ", e.Message);
            EmitSignal(SignalName.OnNetworkError, "Connection error: " + e.Message);
            SetConnectionStatus(false);
            RecordFailure();
        }
    }

    public async void GetPetSnapshot()
    {
        if (!FirebaseConfig.IsConfigured)
            return;

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
            return;

        try
        {
            string userId = authManager.UserId;
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<FirestoreDocument>(body);
                if (doc?.Fields != null)
                {
                    var snapshot = ParseSnapshot(doc);
                    if (snapshot != null)
                    {
                        LastReceivedSnapshot = snapshot;
                        EmitSignal(SignalName.OnSnapshotUpdated);
                        SetConnectionStatus(true);
                        RecordSuccess();
                    }
                    else
                    {
                        RecordFailure();
                    }
                }
                else
                {
                    RecordFailure();
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                SetConnectionStatus(true);
                RecordSuccess();
            }
            else if ((int)response.StatusCode == 429)
            {
                GD.PrintErr("[FirestoreService] Rate limited (429), backing off for ", RateLimitRetryDelaySeconds, "s");
                _isRateLimited = true;
                _pollTimer.Stop();
                _pollTimer.WaitTime = RateLimitRetryDelaySeconds;
                _pollTimer.Start();
                RecordFailure();
                EmitSignal(SignalName.OnNetworkError, "Rate limited by Firestore");
            }
            else
            {
                SetConnectionStatus(false);
                RecordFailure();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Read error: ", e.Message);
            SetConnectionStatus(false);
            RecordFailure();
        }
    }

    public void StartListening()
    {
        if (_isListening) return;
        _isListening = true;
        ResetBackoff();
        _pollTimer.Start();
        GetPetSnapshot();
        _pollingStartTime = DateTime.UtcNow;
        _totalPolls = 0;
        _totalPollSuccesses = 0;
        _totalPollFailures = 0;
        GD.Print("[FirestoreService] Started listening (polling every ", _currentPollInterval, "s)");
    }

    public void StopListening()
    {
        _isListening = false;
        _pollTimer.Stop();
        LogPollMetrics();
        GD.Print("[FirestoreService] Stopped listening");
    }

    private void OnPollTick()
    {
        if (_isListening)
        {
            _totalPolls++;
            GetPetSnapshot();
            _pollTimer.Start();
        }
    }

    private void RecordSuccess()
    {
        _consecutiveFailures = 0;
        _consecutiveSuccesses++;
        _totalPollSuccesses++;
        if (_isRateLimited)
        {
            _isRateLimited = false;
            GD.Print("[FirestoreService] Rate limit cleared");
        }
        if (_consecutiveSuccesses >= SuccessesToResetBackoff && _currentPollInterval > PollIntervalMinSeconds)
        {
            _currentPollInterval = PollIntervalMinSeconds;
            _pollTimer.WaitTime = _currentPollInterval;
            GD.Print("[FirestoreService] Poll backoff reset to ", _currentPollInterval, "s");
        }
    }

    private void RecordFailure()
    {
        _consecutiveSuccesses = 0;
        _consecutiveFailures++;
        _totalPollFailures++;
        float newInterval = _currentPollInterval * PollBackoffMultiplier;
        if (newInterval > PollIntervalMaxSeconds)
            newInterval = PollIntervalMaxSeconds;
        if (newInterval != _currentPollInterval)
        {
            _currentPollInterval = newInterval;
            _pollTimer.WaitTime = _currentPollInterval;
            GD.Print("[FirestoreService] Poll backoff increased to ", _currentPollInterval, "s (failures: ", _consecutiveFailures, ")");
        }
    }

    private void ResetBackoff()
    {
        _currentPollInterval = PollIntervalMinSeconds;
        _consecutiveFailures = 0;
        _consecutiveSuccesses = 0;
        _pollTimer.WaitTime = _currentPollInterval;
    }

    private void LogPollMetrics()
    {
        TimeSpan elapsed = DateTime.UtcNow - _pollingStartTime;
        float successRate = _totalPolls > 0 ? (_totalPollSuccesses / (float)_totalPolls) * 100 : 0;
        GD.Print("[FirestoreService] Poll metrics: polls=", _totalPolls, " success=", _totalPollSuccesses, " fail=", _totalPollFailures, " rate=", successRate.ToString("F1"), "% elapsed=", elapsed.TotalSeconds.ToString("F0"), "s");
    }

    public async void WriteConfirmation(string transferId)
    {
        GD.Print("[FirestoreService] Writing confirmation for transferId=", transferId);
        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string userId = authManager.UserId;
        string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}";

        var doc = new FirestoreDocument
        {
            Fields = new FirestoreFields
            {
                { "TransferId", new FirestoreValue { StringValue = transferId } },
                { "Status", new FirestoreValue { StringValue = "confirmed" } }
            }
        };

        string json = JsonSerializer.Serialize(doc);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bearer {authManager.IdToken}");

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                GD.Print("[FirestoreService] Confirmation written successfully");
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                if ((int)response.StatusCode == 429)
                {
                    GD.PrintErr("[FirestoreService] Rate limited on confirmation (429), backing off");
                    _isRateLimited = true;
                    _pollTimer.Stop();
                    _pollTimer.WaitTime = RateLimitRetryDelaySeconds;
                    if (_isListening) _pollTimer.Start();
                }
                GD.PrintErr("[FirestoreService] Failed to write confirmation: ", response.StatusCode, " ", body);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Confirmation write error: ", e.Message);
        }
    }

    public async void ClearSnapshot()
    {
        if (!FirebaseConfig.IsConfigured)
            return;

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
            return;

        try
        {
            string userId = authManager.UserId;
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}";

            var doc = new FirestoreDocument
            {
                Fields = new FirestoreFields
                {
                    { "TransferId", new FirestoreValue { StringValue = "" } },
                    { "Status", new FirestoreValue { StringValue = "" } },
                    { "PetLocation", new FirestoreValue { StringValue = "" } },
                    { "TransferredAt", new FirestoreValue { StringValue = "" } },
                    { "ConfirmedBy", new FirestoreValue { StringValue = "" } },
                    { "Pet", new FirestoreValue { MapValue = new FirestoreMap { Fields = new FirestoreFields() } } },
                    { "Inventory", new FirestoreValue { MapValue = new FirestoreMap { Fields = new FirestoreFields() } } },
                    { "Version", new FirestoreValue { IntegerValue = "0" } }
                }
            };

            string json = JsonSerializer.Serialize(doc);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                GD.Print("[FirestoreService] Snapshot cleared successfully");
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                GD.PrintErr("[FirestoreService] Failed to clear snapshot: ", response.StatusCode, " ", body);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Clear snapshot error: ", e.Message);
        }
    }

    private void SetConnectionStatus(bool connected)
    {
        if (_isConnected != connected)
        {
            _isConnected = connected;
            EmitSignal(SignalName.OnConnectionStatusChanged, connected);
        }
    }

    private PetSnapshot ParseSnapshot(FirestoreDocument doc)
    {
        try
        {
            var fields = doc.Fields;
            var snapshot = new PetSnapshot();

            if (fields.TryGetValue("Version", out var v) && !string.IsNullOrEmpty(v.IntegerValue))
            {
                int.TryParse(v.IntegerValue, out int version);
                snapshot.Version = version;
            }
            if (fields.TryGetValue("TransferId", out var tid) && tid.StringValue != null)
                snapshot.TransferId = tid.StringValue;
            if (fields.TryGetValue("PetLocation", out var pl) && pl.StringValue != null)
                snapshot.PetLocation = pl.StringValue;
            if (fields.TryGetValue("Status", out var st) && st.StringValue != null)
                snapshot.Status = st.StringValue;
            if (fields.TryGetValue("TransferredAt", out FirestoreValue ta) && ta.StringValue != null)
            {
                DateTime.TryParse(ta.StringValue, out DateTime transferredAt);
                snapshot.TransferredAt = transferredAt;
            }

            if (fields.TryGetValue("Pet", out var petVal) && petVal.MapValue?.Fields != null)
            {
                var pf = petVal.MapValue.Fields;
                snapshot.Pet = new PetState();
                if (pf.TryGetValue("Hunger", out var h) && h.DoubleValue.HasValue)
                    snapshot.Pet.Hunger = (float)h.DoubleValue.Value;
                if (pf.TryGetValue("Happiness", out var hp) && hp.DoubleValue.HasValue)
                    snapshot.Pet.Happiness = (float)hp.DoubleValue.Value;
                if (pf.TryGetValue("Energy", out var e) && e.DoubleValue.HasValue)
                    snapshot.Pet.Energy = (float)e.DoubleValue.Value;
                if (pf.TryGetValue("LastUpdatedAt", out FirestoreValue lu) && lu.StringValue != null)
                {
                    DateTime.TryParse(lu.StringValue, out DateTime lat);
                    snapshot.Pet.LastUpdatedAt = lat;
                }
            }

            if (fields.TryGetValue("Inventory", out var invVal) && invVal.MapValue?.Fields != null)
            {
                var inf = invVal.MapValue.Fields;
                snapshot.Inventory = new InventoryData();
                if (inf.TryGetValue("StarCoins", out var sc) && !string.IsNullOrEmpty(sc.IntegerValue))
                {
                    int.TryParse(sc.IntegerValue, out int coins);
                    snapshot.Inventory.StarCoins = coins;
                }
            }

            return snapshot;
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Error parsing snapshot: ", e.Message);
            return null;
        }
    }
}

public class FirestoreDocument
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Name { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public FirestoreFields Fields { get; set; }
}

public class FirestoreFields : System.Collections.Generic.Dictionary<string, FirestoreValue> { }

public class FirestoreValue
{
    [System.Text.Json.Serialization.JsonPropertyName("stringValue")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string StringValue { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("integerValue")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string IntegerValue { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("doubleValue")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double? DoubleValue { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("mapValue")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public FirestoreMap MapValue { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("arrayValue")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public FirestoreArray ArrayValue { get; set; }
}

public class FirestoreMap
{
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public FirestoreFields Fields { get; set; }
}

public class FirestoreArray
{
    [System.Text.Json.Serialization.JsonPropertyName("values")]
    public System.Collections.Generic.List<FirestoreValue> Values { get; set; } = new();
}
