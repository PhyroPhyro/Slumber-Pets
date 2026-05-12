using Godot;
using System;
public partial class SaveManager : Node
{
private const string SaveFileName = "user://saves/save.json";
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
public void SaveGame(SaveData save)
{
try
{
string dir = SaveFileName.Substring(0, SaveFileName.LastIndexOf('/'));
if (!DirAccess.DirExistsAbsolute(dir))
{
DirAccess.MakeDirAbsolute(dir);
}
string json = JsonSerialization.ToJson(save, true);
using FileAccess file = FileAccess.Open(SaveFileName, FileAccess.ModeFlags.Write);
if (file == null)
{
GD.PrintErr("[SaveManager] Failed to open file for writing: ", SaveFileName);
return;
}
file.StoreString(json);
GD.Print("[SaveManager] Save written successfully");
}
catch (Exception e)
{
GD.PrintErr("[SaveManager] Error saving game: ", e.Message);
GD.PrintErr("[SaveManager] Stack: ", e.StackTrace);
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
}
public class WalkReportData
{
public string Date { get; set; } = string.Empty;
public int Steps { get; set; } = 0;
public int MilestonesReached { get; set; } = 0;
public System.Collections.Generic.List<ItemData> ItemsFound { get; set; } = new();
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