using Godot;
using System;
using System.IO;

public static class FirebaseConfig
{
    private const string ConfigPath = "user://firebase_config.json";

    public static string ApiKey { get; private set; } = "";
    public static string ProjectId { get; private set; } = "";

    public static bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(ProjectId);

    public static void Load()
    {
        if (Godot.FileAccess.FileExists(ConfigPath))
        {
            try
            {
                using Godot.FileAccess file = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                var config = JsonSerialization.FromJson<FirebaseConfigData>(json);
                if (config != null)
                {
                    ApiKey = config.ApiKey ?? "";
                    ProjectId = config.ProjectId ?? "";
                    GD.Print("[FirebaseConfig] Loaded from user://firebase_config.json");
                    return;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("[FirebaseConfig] Error loading config: ", e.Message);
            }
        }

        string bundledPath = "res://data/firebase_config.json";
        if (Godot.FileAccess.FileExists(bundledPath))
        {
            try
            {
                using Godot.FileAccess file = Godot.FileAccess.Open(bundledPath, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                var config = JsonSerialization.FromJson<FirebaseConfigData>(json);
                if (config != null)
                {
                    ApiKey = config.ApiKey ?? "";
                    ProjectId = config.ProjectId ?? "";
                    GD.Print("[FirebaseConfig] Loaded from bundled res://data/firebase_config.json");
                    return;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("[FirebaseConfig] Error loading bundled config: ", e.Message);
            }
        }

        string envKey = System.Environment.GetEnvironmentVariable("FIREBASE_API_KEY");
        string envProject = System.Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
        if (!string.IsNullOrEmpty(envKey) && !string.IsNullOrEmpty(envProject))
        {
            ApiKey = envKey;
            ProjectId = envProject;
            GD.Print("[FirebaseConfig] Loaded from environment variables");
            return;
        }

        GD.PrintErr("[FirebaseConfig] No Firebase configuration found. Set FIREBASE_API_KEY and FIREBASE_PROJECT_ID env vars, or create user://firebase_config.json or res://data/firebase_config.json");
    }

    public static void Save(string apiKey, string projectId)
    {
        ApiKey = apiKey;
        ProjectId = projectId;

        try
        {
            var config = new FirebaseConfigData { ApiKey = apiKey, ProjectId = projectId };
            string json = JsonSerialization.ToJson(config, true);
            using Godot.FileAccess file = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            GD.Print("[FirebaseConfig] Config saved to user://firebase_config.json");
        }
        catch (Exception e)
        {
            GD.PrintErr("[FirebaseConfig] Error saving config: ", e.Message);
        }
    }
}

public class FirebaseConfigData
{
    public string ApiKey { get; set; } = "";
    public string ProjectId { get; set; } = "";
}
