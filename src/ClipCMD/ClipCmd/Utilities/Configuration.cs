using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClipCmd.Utilities;

internal static class Configuration
{
    public static string ApplicationDataPath
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoneRed", "ClipCMD");
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StoneRed", "ClipCMD");
        }
    }

    public static string SettingsFilePath => Path.Combine(ApplicationDataPath, "settings.json");
    public static string LogFilePath => Path.Combine(ApplicationDataPath, $"ClipCMD.log");
}