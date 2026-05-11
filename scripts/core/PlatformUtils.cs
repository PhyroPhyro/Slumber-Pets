using Godot;
using System;

public static class PlatformUtils
{
    public static bool IsAndroidBuild()
    {
        return OS.GetName() == "Android" || OS.HasFeature("Android");
    }

    public static bool IsDesktopBuild()
    {
        string osName = OS.GetName();
        return osName == "Windows" || osName == "X11" || osName == "macOS" ||
               OS.HasFeature("Windows") || OS.HasFeature("Linux") || OS.HasFeature("macOS");
    }
}
