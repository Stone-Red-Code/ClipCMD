using Avalonia.Controls;
using Avalonia.Input.Platform;

using ClipCmd.Models;

using SharpHook;
using SharpHook.Native;

using System.Collections.Concurrent;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ClipCmd.Utilities;

public class ClipCmdCommandHandler(Window window)
{
    private readonly IClipboard clipboard = window.Clipboard!;
    private readonly EventSimulator simulator = new EventSimulator();
    private readonly ConcurrentDictionary<string, ClipCmdCommand> commands = new();
    private string lastText = string.Empty;

    public void Start()
    {
        _ = new TaskFactory().StartNew(async () =>
        {
            while (true)
            {
                await Run();
                await Task.Delay(100);
            }
        }, TaskCreationOptions.LongRunning);
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

        string commandName = text[Settings.Current.Prefix.Length..^Settings.Current.Suffix.Length];

        StringBuilder outText = new StringBuilder();

        if (commands.TryGetValue(commandName, out ClipCmdCommand? command))
        {
            PowerShell powerShell = PowerShell.Create();
            _ = powerShell.AddScript(command.Script);

            foreach (PSObject commandResult in await powerShell.InvokeAsync())
            {
                _ = outText.AppendLine(commandResult?.ToString() ?? string.Empty);
            }
        }
        else if (string.IsNullOrWhiteSpace(commandName) || string.IsNullOrEmpty(Settings.Current.Suffix + Settings.Current.Prefix))
        {
            return;
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
                _ = simulator.SimulateTextEntry(c.ToString());
                await Task.Delay(Settings.Current.AutoTypeDelay);
            }
        }

        lastText = string.Empty;
    }
}