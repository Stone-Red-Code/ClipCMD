using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input.Platform;

using ClipCmd.Models;

using SharpHook;
using SharpHook.Native;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClipCmd.Utilities;

public class ClipCmdCommandHandler(Window window)
{
    private readonly IClipboard clipboard = window.Clipboard!;
    private readonly EventSimulator simulator = new EventSimulator();
    private string lastText = string.Empty;
    public AvaloniaDictionary<string, ClipCmdCommand> Commands { get; } = [];

    public void Start()
    {
        _ = new TaskFactory().StartNew(async () =>
        {
            LoadCommands();

            while (true)
            {
                await Run();
                await Task.Delay(100);
            }
        }, TaskCreationOptions.LongRunning);
    }

    public void ClearCommands()
    {
        Commands.Clear();
    }

    public void SaveCommands()
    {
        string json = JsonSerializer.Serialize(Commands);
        File.WriteAllText(Configuration.CommandsPath, json);
    }

    public void LoadCommands()
    {
        if (!File.Exists(Configuration.CommandsPath))
        {
            return;
        }

        string json = File.ReadAllText(Configuration.CommandsPath);
        Commands.Clear();

        foreach (KeyValuePair<string, ClipCmdCommand> command in JsonSerializer.Deserialize<ConcurrentDictionary<string, ClipCmdCommand>>(json) ?? new())
        {
            Commands[command.Key] = command.Value;
        }
    }

    private async Task Run()
    {
        string? text = await clipboard.GetTextAsync();

        if (text is null || text == lastText)
        {
            return;
        }

        lastText = text;

        if (!text.StartsWith(Settings.Current.Prefix) || !text.EndsWith(Settings.Current.Suffix))
        {
            return;
        }

        string input = text[Settings.Current.Prefix.Length..^Settings.Current.Suffix.Length].Replace(Settings.Current.CommandArgsSeperator + Settings.Current.CommandArgsSeperator, "\0");
        string[] parts = input.Split(Settings.Current.CommandArgsSeperator);
        string commandName = parts[0].Replace("\0", Settings.Current.CommandArgsSeperator);
        string[] parameters = parts.Skip(1).Select(p => p.Replace("\0", Settings.Current.CommandArgsSeperator)).ToArray();

        StringBuilder outText = new StringBuilder();

        if (Commands.TryGetValue(commandName, out ClipCmdCommand? command))
        {
            PowerShell powerShell = PowerShell.Create();
            _ = powerShell.AddScript(command.Script);
            _ = powerShell.AddParameters(parameters);

            foreach (PSObject commandResult in await powerShell.InvokeAsync())
            {
                _ = outText.AppendLine(commandResult?.ToString() ?? string.Empty);
            }
        }
        else if (string.IsNullOrWhiteSpace(commandName) || string.IsNullOrEmpty(Settings.Current.Suffix + Settings.Current.Prefix))
        {
            return;
        }
        else if (commandName == "list")
        {
            _ = outText.AppendLine($"Commands: {string.Join(", ", Commands.Keys)}");
        }
        else
        {
            _ = outText.AppendLine("Command not found!");
        }

        await clipboard.ClearAsync();

        if (Settings.Current.Mode == ClipCmdMode.Clipboard)
        {
            await clipboard.SetTextAsync(outText.ToString().TrimEnd());

            if (Settings.Current.AutoPaste)
            {
                _ = simulator.SimulateKeyPress(KeyCode.VcLeftControl);
                _ = simulator.SimulateKeyPress(KeyCode.VcV);

                _ = simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
                _ = simulator.SimulateKeyRelease(KeyCode.VcV);
            }
        }
        else
        {
            foreach (char c in outText.ToString().TrimEnd())
            {
                if (c == '\n')
                {
                    _ = simulator.SimulateKeyPress(KeyCode.VcEnter);
                    continue;
                }

                _ = simulator.SimulateTextEntry(c.ToString());
                await Task.Delay(Settings.Current.AutoTypeDelay);
            }
        }

        lastText = string.Empty;
    }
}