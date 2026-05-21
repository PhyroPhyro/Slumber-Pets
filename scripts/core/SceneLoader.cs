using Godot;
using System;

public partial class SceneLoader : Node
{
    public override void _Ready()
    {
        GD.Print("[SceneLoader] Detecting platform...");
        GD.Print("[SceneLoader] OS.GetName() = ", OS.GetName());
        GD.Print("[SceneLoader] OS.HasFeature(Android) = ", OS.HasFeature("Android"));

        var gameManager = GetNode<GameManager>("/root/GameManager");

        if (!gameManager.CurrentSave.IsPetNamed)
        {
            GD.Print("[SceneLoader] First boot detected - showing pet naming scene");
            CallDeferred(MethodName.ChangeScene, "res://scenes/ui/pet_name.tscn");
            return;
        }

        string targetScene;

        if (PlatformUtils.IsAndroidBuild())
        {
            targetScene = "res://scenes/main_mobile.tscn";
            GD.Print("[SceneLoader] Android detected - loading mobile scene");
        }
        else
        {
            targetScene = "res://scenes/main_desktop.tscn";
            GD.Print("[SceneLoader] Desktop detected - loading desktop scene");
        }

        CallDeferred(MethodName.ChangeScene, targetScene);
    }

    private void ChangeScene(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}
