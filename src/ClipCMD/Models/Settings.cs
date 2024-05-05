using ClipCmd.Utilities;

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ClipCmd.Models;

public class Settings
{
    public static Settings Current { get; } = LoadSettings();

    public bool AutoPaste { get; set; } = true;

    public int AutoTypeDelay { get; set; }

    public ClipCmdMode Mode { get; set; }

    public string Prefix { get; set; } = "#";

    public string Suffix { get; set; } = "";

    public static void SaveSettings()
    {
        if (!Directory.Exists(Configuration.ApplicationDataPath))
        {
            _ = Directory.CreateDirectory(Configuration.ApplicationDataPath);
        }

        File.WriteAllText(Configuration.SettingsFilePath, JsonSerializer.Serialize(Current));
    }

    private static Settings LoadSettings()
    {
        if (File.Exists(Configuration.SettingsFilePath))
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Configuration.SettingsFilePath)) ?? new Settings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new Settings();
            }
        }
        else
        {
            return new Settings();
        }
    }
}