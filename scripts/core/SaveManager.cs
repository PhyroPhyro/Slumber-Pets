using Godot;
using System;
using System.Security.Cryptography;
using System.Text;
public partial class SaveManager : Node
{
private const string SaveFileName = "user://saves/save.json";
private const string BackupFileName = "user://saves/save_backup.json";
private const string QueueFileName = "user://saves/queue.json";
private const int CurrentVersion = 1;
public static SaveData CreateDefaultSave()
{
return new SaveData
{
Version = CurrentVersion,
PetLocation = "pc",
LastTransferId = string.Empty,
Pet = new PetState(100f, 100f, 100f, DateTime.UtcNow),
Inventory = new InventoryData { StarCoins = 0, Items = new System.Collections.Generic.List<ItemData>() },
SleepHistory = new System.Collections.Generic.List<SleepReportData>(),
WalkHistory = new System.Collections.Generic.List<WalkReportData>()
};
}
public SaveData LoadSave()
{
if (!FileAccess.FileExists(SaveFileName))
{
GD.Print("[SaveManager] No save file found, creating default save");
SaveData defaultSave = CreateDefaultSave();
SaveGame(defaultSave);
return defaultSave;
}
try
{
using FileAccess file = FileAccess.Open(SaveFileName, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
SaveData save = JsonSerialization.FromJson<SaveData>(json);
if (save == null || save.Version != CurrentVersion)
{
GD.PrintErr("[SaveManager] Invalid save version, creating default save");
SaveData defaultSave = CreateDefaultSave();
SaveGame(defaultSave);
return defaultSave;
}

if (!string.IsNullOrEmpty(save.Checksum))
{
string expectedChecksum = save.Checksum;
save.Checksum = null;
string actualChecksum = ComputeChecksum(save);
save.Checksum = expectedChecksum;

if (expectedChecksum != actualChecksum)
{
GD.PrintErr("[SaveManager] Save integrity check failed! Attempting to restore from backup...");
if (FileAccess.FileExists(BackupFileName))
{
using FileAccess backupFile = FileAccess.Open(BackupFileName, FileAccess.ModeFlags.Read);
string backupJson = backupFile.GetAsText();
SaveData backupSave = JsonSerialization.FromJson<SaveData>(backupJson);
if (backupSave != null && backupSave.Version == CurrentVersion)
{
GD.Print("[SaveManager] Backup restored successfully");
return backupSave;
}
}
GD.PrintErr("[SaveManager] Backup also invalid or missing, creating default save");
SaveData defaultSave = CreateDefaultSave();
SaveGame(defaultSave);
return defaultSave;
}
GD.Print("[SaveManager] Save integrity verified");
}

GD.Print("[SaveManager] Save loaded successfully");
return save;
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Error loading save: ", e.Message);
SaveData defaultSave = CreateDefaultSave();
SaveGame(defaultSave);
return defaultSave;
}
}

private string ComputeChecksum(SaveData save)
{
string data = JsonSerialization.ToJson(save, false);
using SHA256 sha256 = SHA256.Create();
byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
StringBuilder sb = new StringBuilder();
foreach (byte b in bytes)
{
sb.Append(b.ToString("x2"));
}
return sb.ToString();
}
public void SaveGame(SaveData save)
{
try
{
string dir = SaveFileName.Substring(0, SaveFileName.LastIndexOf('/'));
if (!DirAccess.DirExistsAbsolute(dir))
{
DirAccess.MakeDirAbsolute(dir);
}

CreateBackup();

save.Checksum = ComputeChecksum(save);

string json = JsonSerialization.ToJson(save, true);
using FileAccess file = FileAccess.Open(SaveFileName, FileAccess.ModeFlags.Write);
if (file == null)
{
GD.PrintErr("[SaveManager] Failed to open file for writing: ", SaveFileName);
return;
}
file.StoreString(json);
GD.Print("[SaveManager] Save written successfully (checksum: ", save.Checksum, ")");
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Error saving game: ", e.Message);
GD.PrintErr("[SaveManager] Stack: ", e.StackTrace);
}
}

private void CreateBackup()
{
if (FileAccess.FileExists(SaveFileName))
{
try
{
using FileAccess source = FileAccess.Open(SaveFileName, FileAccess.ModeFlags.Read);
string content = source.GetAsText();
using FileAccess backup = FileAccess.Open(BackupFileName, FileAccess.ModeFlags.Write);
if (backup != null)
{
backup.StoreString(content);
GD.Print("[SaveManager] Backup created");
}
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Failed to create backup: ", e.Message);
}
}
}
    public void ResetSave()
    {
        try
        {
            if (FileAccess.FileExists(SaveFileName))
            {
                DirAccess.RemoveAbsolute(SaveFileName);
                GD.Print("[SaveManager] Save file deleted");
            }
            SaveData defaultSave = CreateDefaultSave();
            SaveGame(defaultSave);
            GD.Print("[SaveManager] Save reset to default");
        }
        catch (Exception e)
        {
            GD.PrintErr("[SaveManager] Error resetting save: ", e.Message);
        }
    }
    public void ApplyOfflineDecay(SaveData save)
{
if (save.Pet == null) return;
TimeSpan elapsed = DateTime.UtcNow - save.Pet.LastUpdatedAt;
if (elapsed.TotalMinutes > 1)
{
GD.Print("[SaveManager] Applying offline decay for ", elapsed.TotalHours.ToString("F2"), " hours");
save.Pet.ApplyOfflineDecay(elapsed);
}
}

public void QueueForSync(string transferId, string targetLocation)
{
try
{
string dir = QueueFileName.Substring(0, QueueFileName.LastIndexOf('/'));
if (!DirAccess.DirExistsAbsolute(dir))
{
DirAccess.MakeDirAbsolute(dir);
}

SyncQueueItem item = new SyncQueueItem
{
TransferId = transferId,
TargetLocation = targetLocation,
QueuedAt = DateTime.UtcNow.ToString("o")
};

System.Collections.Generic.List<SyncQueueItem> queue = new System.Collections.Generic.List<SyncQueueItem>();
if (FileAccess.FileExists(QueueFileName))
{
using FileAccess file = FileAccess.Open(QueueFileName, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
queue = JsonSerialization.FromJson<System.Collections.Generic.List<SyncQueueItem>>(json) ?? queue;
}

queue.Add(item);

string newJson = JsonSerialization.ToJson(queue, true);
using FileAccess writeFile = FileAccess.Open(QueueFileName, FileAccess.ModeFlags.Write);
if (writeFile != null)
{
writeFile.StoreString(newJson);
GD.Print("[SaveManager] Queued sync for transferId=", transferId);
}
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Failed to queue for sync: ", e.Message);
}
}

public System.Collections.Generic.List<SyncQueueItem> ProcessSyncQueue()
{
try
{
if (!FileAccess.FileExists(QueueFileName))
{
return new System.Collections.Generic.List<SyncQueueItem>();
}

using FileAccess file = FileAccess.Open(QueueFileName, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var queue = JsonSerialization.FromJson<System.Collections.Generic.List<SyncQueueItem>>(json);

if (queue != null && queue.Count > 0)
{
GD.Print("[SaveManager] Processing sync queue: ", queue.Count, " items pending");
return queue;
}
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Failed to process sync queue: ", e.Message);
}

return new System.Collections.Generic.List<SyncQueueItem>();
}

public void ClearSyncQueue()
{
try
{
if (FileAccess.FileExists(QueueFileName))
{
DirAccess.RemoveAbsolute(QueueFileName);
GD.Print("[SaveManager] Sync queue cleared");
}
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Failed to clear sync queue: ", e.Message);
}
}
}
public class SaveData
{
public int Version { get; set; }
public string PetLocation { get; set; } = "pc";
public string LastTransferId { get; set; } = string.Empty;
public PetState Pet { get; set; } = new();
public InventoryData Inventory { get; set; } = new();
public System.Collections.Generic.List<SleepReportData> SleepHistory { get; set; } = new();
public System.Collections.Generic.List<WalkReportData> WalkHistory { get; set; } = new();
public string Checksum { get; set; }
}
public class InventoryData
{
public int StarCoins { get; set; } = 0;
public System.Collections.Generic.List<ItemData> Items { get; set; } = new();
}
public class ItemData
{
public string Id { get; set; } = string.Empty;
public int Qty { get; set; } = 0;
}
public class SleepReportData
{
public string Date { get; set; } = string.Empty;
public int DurationMin { get; set; } = 0;
public int Score { get; set; } = 0;
public string Tier { get; set; } = string.Empty;
public int CoinsEarned { get; set; } = 0;
public bool MidnightBonus { get; set; } = false;
public float ConsistencyBonus { get; set; } = 0f;
public string RareItemId { get; set; } = null;
}
public class WalkReportData
{
public string Date { get; set; } = string.Empty;
public int Steps { get; set; } = 0;
public int MilestonesReached { get; set; } = 0;
public System.Collections.Generic.List<ItemData> ItemsFound { get; set; } = new();
}

public class SyncQueueItem
{
public string TransferId { get; set; } = string.Empty;
public string TargetLocation { get; set; } = string.Empty;
public string QueuedAt { get; set; } = string.Empty;
}
public static class JsonSerialization
{
public static string ToJson(object obj, bool prettyPrint = false)
{
var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = prettyPrint };
return System.Text.Json.JsonSerializer.Serialize(obj, options);
}
public static T FromJson<T>(string json)
{
return System.Text.Json.JsonSerializer.Deserialize<T>(json);
}
}