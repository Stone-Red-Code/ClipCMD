﻿using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input.Platform;

using ClipCmd.Models;

using SharpHook;
using SharpHook.Native;

using System;
using System.Collections;
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
        Hashtable? returnAction = null;

        if (Commands.TryGetValue(commandName, out ClipCmdCommand? command))
        {
            PowerShell powerShell = PowerShell.Create();
            _ = powerShell.AddScript(command.Script);
            _ = powerShell.AddParameters(parameters);

            PSDataCollection<PSObject> commandResults = await powerShell.InvokeAsync();

            foreach (PSObject commandResult in commandResults)
            {
                // check if commandResult is string

                if (commandResult.TypeNames.Contains("System.String"))
                {
                    _ = outText.AppendLine(commandResult.ToString());
                }
                else if (commandResult.BaseObject is Hashtable hashtable)
                {
                    if (commandResult == commandResults.LastOrDefault())
                    {
                        returnAction = hashtable;
                        continue;
                    }

                    await ProcessActions(hashtable);
                }
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
                await Task.Delay(50);
                _ = simulator.SimulateKeyPress(KeyCode.VcLeftControl);
                _ = simulator.SimulateKeyPress(KeyCode.VcV);

                _ = simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
                _ = simulator.SimulateKeyRelease(KeyCode.VcV);
                await Task.Delay(50);
            }
        }
        else
        {
            _ = simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            _ = simulator.SimulateKeyRelease(KeyCode.VcRightControl);
            _ = simulator.SimulateKeyRelease(KeyCode.VcLeftAlt);
            _ = simulator.SimulateKeyRelease(KeyCode.VcRightAlt);

            foreach (char c in outText.ToString().TrimEnd())
            {
                await Task.Delay(Settings.Current.AutoTypeDelay);
                if (c == '\n')
                {
                    _ = simulator.SimulateKeyPress(KeyCode.VcEnter);
                    continue;
                }

                _ = simulator.SimulateTextEntry(c.ToString());
            }
        }

        if (returnAction is not null)
        {
            await ProcessActions(returnAction);
        }

        lastText = string.Empty;
    }

    private async Task ProcessActions(Hashtable hashtable)
    {
        if (!hashtable.ContainsKey("Action") || hashtable["Action"] is not string action)
        {
            return;
        }

        if (action == "Paste")
        {
            if (!hashtable.ContainsKey("Text") || hashtable["Text"] is not string text)
            {
                return;
            }

            await clipboard.SetTextAsync(text);
            await Task.Delay(50);
            _ = simulator.SimulateKeyPress(KeyCode.VcLeftControl);
            _ = simulator.SimulateKeyPress(KeyCode.VcV);

            _ = simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            _ = simulator.SimulateKeyRelease(KeyCode.VcV);
            await Task.Delay(50);
        }
        else if (action == "Type")
        {
            if (!hashtable.ContainsKey("Text") || hashtable["Text"] is not string text)
            {
                return;
            }

            _ = simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            _ = simulator.SimulateKeyRelease(KeyCode.VcRightControl);
            _ = simulator.SimulateKeyRelease(KeyCode.VcLeftAlt);
            _ = simulator.SimulateKeyRelease(KeyCode.VcRightAlt);

            foreach (char c in text)
            {
                await Task.Delay(Settings.Current.AutoTypeDelay);
                if (c == '\n')
                {
                    _ = simulator.SimulateKeyPress(KeyCode.VcEnter);
                    continue;
                }

                _ = simulator.SimulateTextEntry(c.ToString());
            }
        }
        else if (action == "KeyPress")
        {
            if (!hashtable.ContainsKey("Key") || !Enum.TryParse(hashtable["Key"]?.ToString(), out KeyCode key))
            {
                return;
            }

            _ = simulator.SimulateKeyPress(key);
        }
        else if (action == "KeyRelease")
        {
            if (!hashtable.ContainsKey("Key") || !Enum.TryParse(hashtable["Key"]?.ToString(), out KeyCode key))
            {
                return;
            }

            _ = simulator.SimulateKeyRelease(key);
        }
        else if (action == "KeyStroke")
        {
            if (!hashtable.ContainsKey("Key") || !Enum.TryParse(hashtable["Key"]?.ToString(), out KeyCode key))
            {
                return;
            }

            _ = simulator.SimulateKeyPress(key);
            _ = simulator.SimulateKeyRelease(key);
        }
        else if (action == "Delay")
        {
            if (!hashtable.ContainsKey("Delay") || hashtable["Delay"] is not int delay)
            {
                return;
            }

            await Task.Delay(delay);
        }
    }
}