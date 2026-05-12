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
    private const float PollIntervalSeconds = 5.0f;

    public PetSnapshot LastReceivedSnapshot { get; private set; }
    public bool IsListening => _isListening;

    public override void _Ready()
    {
        _httpClient = new System.Net.Http.HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);

        _pollTimer = new Timer();
        _pollTimer.WaitTime = PollIntervalSeconds;
        _pollTimer.OneShot = false;
        _pollTimer.Timeout += OnPollTick;
        AddChild(_pollTimer);

        FirebaseConfig.Load();
        GD.Print("[FirestoreService] Initialized (poll interval: ", PollIntervalSeconds, "s)");
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
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}/petSnapshot";

            var doc = new FirestoreDocument
            {
                Fields = new FirestoreFields
                {
                    { "Version", new FirestoreValue { IntegerValue = snapshot.Version } },
                    { "TransferId", new FirestoreValue { StringValue = snapshot.TransferId } },
                    { "PetLocation", new FirestoreValue { StringValue = snapshot.PetLocation } },
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
                        { "StarCoins", new FirestoreValue { IntegerValue = snapshot.Inventory.StarCoins } }
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
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                GD.PrintErr("[FirestoreService] Write failed: ", response.StatusCode, " ", body);
                EmitSignal(SignalName.OnNetworkError, "Failed to write snapshot");
                SetConnectionStatus(false);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Write error: ", e.Message);
            EmitSignal(SignalName.OnNetworkError, "Connection error: " + e.Message);
            SetConnectionStatus(false);
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
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}/petSnapshot";

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
                    }
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                SetConnectionStatus(true);
            }
            else
            {
                SetConnectionStatus(false);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Read error: ", e.Message);
            SetConnectionStatus(false);
        }
    }

    public void StartListening()
    {
        if (_isListening) return;
        _isListening = true;
        _pollTimer.Start();
        GetPetSnapshot();
        GD.Print("[FirestoreService] Started listening (polling every ", PollIntervalSeconds, "s)");
    }

    public void StopListening()
    {
        _isListening = false;
        _pollTimer.Stop();
        GD.Print("[FirestoreService] Stopped listening");
    }

    private void OnPollTick()
    {
        if (_isListening)
            GetPetSnapshot();
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

            if (fields.TryGetValue("Version", out var v) && v.IntegerValue.HasValue)
                snapshot.Version = (int)v.IntegerValue.Value;
            if (fields.TryGetValue("TransferId", out var tid) && tid.StringValue != null)
                snapshot.TransferId = tid.StringValue;
            if (fields.TryGetValue("PetLocation", out var pl) && pl.StringValue != null)
                snapshot.PetLocation = pl.StringValue;
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
                if (inf.TryGetValue("StarCoins", out var sc) && sc.IntegerValue.HasValue)
                    snapshot.Inventory.StarCoins = (int)sc.IntegerValue.Value;
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
    public string Name { get; set; }
    public FirestoreFields Fields { get; set; }
}

public class FirestoreFields : System.Collections.Generic.Dictionary<string, FirestoreValue> { }

public class FirestoreValue
{
    public string StringValue { get; set; }
    public long? IntegerValue { get; set; }
    public double? DoubleValue { get; set; }
    public FirestoreMap MapValue { get; set; }
    public FirestoreArray ArrayValue { get; set; }
}

public class FirestoreMap
{
    public FirestoreFields Fields { get; set; }
}

public class FirestoreArray
{
    public System.Collections.Generic.List<FirestoreValue> Values { get; set; } = new();
}
