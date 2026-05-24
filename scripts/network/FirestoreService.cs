using Godot;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class FirestoreService : Node
{
    [Signal] public delegate void OnSnapshotWriteCompleteEventHandler();
    [Signal] public delegate void OnNetworkErrorEventHandler(string reason);
    [Signal] public delegate void OnConnectionStatusChangedEventHandler(bool isConnected);

    private System.Net.Http.HttpClient _httpClient;
    private bool _isConnected = false;

    public void SetLastKnownTransferId(string transferId) { }

    public override void _Ready()
    {
        _httpClient = new System.Net.Http.HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        FirebaseConfig.Load();
        GD.Print("[FirestoreService] Initialized (async transfer mode)");
    }

    public override void _ExitTree()
    {
        _httpClient?.Dispose();
    }

    public async Task<bool> SendPetAsync(PetSnapshot snapshot)
    {
        if (!FirebaseConfig.IsConfigured)
        {
            EmitSignal(SignalName.OnNetworkError, "Firebase not configured");
            return false;
        }

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            EmitSignal(SignalName.OnNetworkError, "Not authenticated");
            return false;
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
                    { "Status", new FirestoreValue { StringValue = "available" } },
                    { "TransferredAt", new FirestoreValue { StringValue = snapshot.TransferredAt.ToString("o") } },
                    { "ClaimedBy", new FirestoreValue { StringValue = "" } },
                    { "ClaimedAt", new FirestoreValue { StringValue = "" } }
                }
            };

            doc.Fields["Pet"] = new FirestoreValue
            {
                MapValue = new FirestoreMap
                {
                    Fields = new FirestoreFields
                    {
                        { "Name", new FirestoreValue { StringValue = snapshot.Pet.Name } },
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
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                GD.Print("[FirestoreService] PetSnapshot written: transferId=", snapshot.TransferId, " status=available");
                EmitSignal(SignalName.OnSnapshotWriteComplete);
                SetConnectionStatus(true);
                return true;
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                GD.PrintErr("[FirestoreService] Write failed: ", response.StatusCode, " ", body);
                EmitSignal(SignalName.OnNetworkError, "Failed to write snapshot");
                SetConnectionStatus(false);
                return false;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Write error: ", e.Message);
            EmitSignal(SignalName.OnNetworkError, "Connection error: " + e.Message);
            SetConnectionStatus(false);
            return false;
        }
    }

    public async Task<RemotePetStatus> CheckRemotePetAsync()
    {
        if (!FirebaseConfig.IsConfigured)
            return RemotePetStatus.Error;

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
            return RemotePetStatus.Error;

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
                if (doc?.Fields != null && doc.Fields.TryGetValue("Status", out var statusVal))
                {
                    string status = statusVal.StringValue ?? "";
                    SetConnectionStatus(true);
                    return status switch
                    {
                        "available" => RemotePetStatus.Available,
                        "claimed" => RemotePetStatus.Claimed,
                        "in_transit" => RemotePetStatus.Claimed,
                        _ => RemotePetStatus.None
                    };
                }
                SetConnectionStatus(true);
                return RemotePetStatus.None;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                SetConnectionStatus(true);
                return RemotePetStatus.None;
            }
            else
            {
                SetConnectionStatus(false);
                return RemotePetStatus.Error;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Check error: ", e.Message);
            SetConnectionStatus(false);
            return RemotePetStatus.Error;
        }
    }

    public async Task<PetSnapshot> ClaimRemotePetAsync()
    {
        if (!FirebaseConfig.IsConfigured)
            return null;

        var authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
        string token = await authManager.GetValidTokenAsync();
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            string userId = authManager.UserId;
            string url = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents/users/{userId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                GD.PrintErr("[FirestoreService] Claim read failed: ", response.StatusCode);
                return null;
            }

            string body = await response.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<FirestoreDocument>(body);

            if (doc?.Fields == null)
                return null;

            if (!doc.Fields.TryGetValue("Status", out var statusVal))
            {
                GD.Print("[FirestoreService] No status field found");
                return null;
            }

            string status = statusVal.StringValue ?? "";

            if (status == "claimed" || status == "in_transit")
            {
                GD.Print("[FirestoreService] Pet not available for claim (status=", status, ")");
                return null;
            }

            if (status != "available")
            {
                GD.Print("[FirestoreService] Unexpected status: ", status);
                return null;
            }

            var snapshot = ParseSnapshot(doc);
            if (snapshot == null)
                return null;

            bool claimed = await TryAtomicClaimAsync(userId, token);
            if (!claimed)
            {
                GD.Print("[FirestoreService] Atomic claim failed (race condition detected)");
                return null;
            }

            GD.Print("[FirestoreService] Pet claimed successfully: transferId=", snapshot.TransferId);
            SetConnectionStatus(true);

            await ClearSnapshotAsync();

            return snapshot;
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirestoreService] Claim error: ", e.Message);
            SetConnectionStatus(false);
            return null;
        }
    }

    private async Task<bool> TryAtomicClaimAsync(string userId, string token)
    {
        string baseUrl = $"https://firestore.googleapis.com/v1/projects/{FirebaseConfig.ProjectId}/databases/(default)/documents";
        string docPath = $"users/{userId}";

        string transactionUrl = $"{baseUrl}:beginTransaction";
        var beginRequest = new HttpRequestMessage(HttpMethod.Post, transactionUrl);
        beginRequest.Headers.Add("Authorization", $"Bearer {token}");
        beginRequest.Content = new StringContent("{\"options\":{}}", Encoding.UTF8, "application/json");

        HttpResponseMessage beginResponse = await _httpClient.SendAsync(beginRequest);
        if (!beginResponse.IsSuccessStatusCode)
        {
            GD.PrintErr("[FirestoreService] Failed to begin transaction");
            return false;
        }

        string beginBody = await beginResponse.Content.ReadAsStringAsync();
        var beginResult = JsonSerializer.Deserialize<TransactionBeginResponse>(beginBody);
        string transactionId = beginResult?.Transaction;

        if (string.IsNullOrEmpty(transactionId))
        {
            GD.PrintErr("[FirestoreService] No transaction ID returned");
            return false;
        }

        string readDocUrl = $"{baseUrl}/{docPath}";
        var readRequest = new HttpRequestMessage(HttpMethod.Get, readDocUrl);
        readRequest.Headers.Add("Authorization", $"Bearer {token}");
        HttpResponseMessage readResponse = await _httpClient.SendAsync(readRequest);

        if (!readResponse.IsSuccessStatusCode)
        {
            GD.PrintErr("[FirestoreService] Transaction read failed");
            return false;
        }

        string readBody = await readResponse.Content.ReadAsStringAsync();
        var readDoc = JsonSerializer.Deserialize<FirestoreDocument>(readBody);

        if (readDoc?.Fields == null || !readDoc.Fields.TryGetValue("Status", out var statusVal))
            return false;

        if (statusVal.StringValue != "available")
        {
            GD.Print("[FirestoreService] Transaction aborted: status changed to ", statusVal.StringValue);
            return false;
        }

        string commitUrl = $"{baseUrl}:commit";
        var commitDoc = new FirestoreDocument
        {
            Fields = new FirestoreFields
            {
                { "Status", new FirestoreValue { StringValue = "claimed" } },
                { "ClaimedBy", new FirestoreValue { StringValue = userId } },
                { "ClaimedAt", new FirestoreValue { StringValue = DateTime.UtcNow.ToString("o") } }
            }
        };

        string currentRevision = ExtractRevision(readBody);

        var commitPayload = new
        {
            transaction = transactionId,
            writes = new object[]
            {
                new
                {
                    update = commitDoc,
                    currentDocument = new { exists = true, updateTime = currentRevision }
                }
            }
        };

        string commitJson = JsonSerializer.Serialize(commitPayload);
        var commitRequest = new HttpRequestMessage(HttpMethod.Post, commitUrl);
        commitRequest.Headers.Add("Authorization", $"Bearer {token}");
        commitRequest.Content = new StringContent(commitJson, Encoding.UTF8, "application/json");

        HttpResponseMessage commitResponse = await _httpClient.SendAsync(commitRequest);

        if (commitResponse.IsSuccessStatusCode)
        {
            GD.Print("[FirestoreService] Atomic claim committed successfully");
            return true;
        }
        else
        {
            string errBody = await commitResponse.Content.ReadAsStringAsync();
            GD.PrintErr("[FirestoreService] Atomic claim failed: ", commitResponse.StatusCode, " ", errBody);
            return false;
        }
    }

    private string ExtractRevision(string documentJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(documentJson);
            if (doc.RootElement.TryGetProperty("updateTime", out var updateTime))
                return updateTime.GetString();
        }
        catch { }
        return "";
    }

    public async Task ClearSnapshotAsync()
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
                    { "ClaimedBy", new FirestoreValue { StringValue = "" } },
                    { "ClaimedAt", new FirestoreValue { StringValue = "" } },
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
                if (pf.TryGetValue("Name", out var n) && n.StringValue != null)
                    snapshot.Pet.Name = n.StringValue;
                if (pf.TryGetValue("Hunger", out var h) && h.DoubleValue.HasValue)
                    snapshot.Pet.Hunger = (float)h.DoubleValue.Value;
                if (pf.TryGetValue("Happiness", out var hp) && hp.DoubleValue.HasValue)
                    snapshot.Pet.Happiness = (float)hp.DoubleValue.Value;
                if (pf.TryGetValue("Energy", out var e) && e.DoubleValue.HasValue)
                    snapshot.Pet.Energy = (float)e.DoubleValue.Value;
                if (pf.TryGetValue("LastUpdatedAt", out FirestoreValue lu) && lu.StringValue != null)
                {
                    DateTime.TryParse(lu.StringValue, out DateTime lastUpdated);
                    snapshot.Pet.LastUpdatedAt = lastUpdated;
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

public enum RemotePetStatus
{
    None,
    Available,
    Claimed,
    Error
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

public class TransactionBeginResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("transaction")]
    public string Transaction { get; set; }
}
