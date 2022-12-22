using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Media;

using WindowsInput;
using WindowsInput.Native;

namespace ClipCMD;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Dictionary<string, string> commands = new Dictionary<string, string>();

    private readonly string cmdPath;
    private readonly string fixPath;

    public MainWindow()
    {
        InitializeComponent();

        string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string workPath = Path.GetDirectoryName(filePath) ?? "/";

        cmdPath = Path.Combine(workPath, "cmd.txt");
        fixPath = Path.Combine(workPath, "fix.txt");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Initialize the clipboard now that we have a window source to use
        ClipboardManager windowClipboardManager = new ClipboardManager(this);
        windowClipboardManager.ClipboardChanged += ClipboardManager_ClipboardChanged;

        if (File.Exists(cmdPath))
        {
            commandsTextBox.Text = File.ReadAllText(cmdPath);
        }

        if (File.Exists(fixPath))
        {
            string[] lines = File.ReadAllLines(fixPath);
            if (lines.Length == 2)
            {
                prefixTextBox.Text = lines[0];
                suffixTextBox.Text = lines[1];
            }
            else
            {
                File.Delete(fixPath);
            }
        }
    }

    private IDataObject? oldData;

    private void ClipboardManager_ClipboardChanged(object? sender, EventArgs e)
    {
        if (!Clipboard.ContainsText() || (oldData is not null && Clipboard.IsCurrent(oldData)))
        {
            return;
        }

        try
        {
            bool result = SafeRepeat.Start(() => Clipboard.GetText().Trim(), 100, out string? clipboardText);

            if (!result || clipboardText is null)
            {
                return;
            }

            if (!clipboardText.StartsWith(prefixTextBox.Text) || !clipboardText.EndsWith(suffixTextBox.Text))
            {
                oldData = Clipboard.GetDataObject();
                return;
            }

            clipboardText = clipboardText[prefixTextBox.Text.Length..^suffixTextBox.Text.Length].Trim();

            if (!commands.TryGetValue(clipboardText, out string? script))
            {
                oldData = Clipboard.GetDataObject();
                return;
            }

            PowerShell ps = PowerShell.Create().AddScript(script);
            StringBuilder outText = new StringBuilder();

            foreach (PSObject commandResult in ps.Invoke())
            {
                _ = outText.AppendLine(commandResult.ToString());
            }

            logListBox.Items.Insert(0, $"{clipboardText} > {outText.ToString().TrimEnd()}");

            Clipboard.SetText(outText.ToString().TrimEnd());

            oldData = Clipboard.GetDataObject();

            InputSimulator inputSimulator = new InputSimulator();
            _ = inputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL }, new[] { VirtualKeyCode.VK_V });
        }
        catch (Exception ex)
        {
            logListBox.Items.Insert(0, $"Error: {ex.Message}");
            Debug.WriteLine(ex);
        }
    }

    private void StaticCommandsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        //TODO parse commands

        string[] lines = commandsTextBox.Text.Split('\n').Append("[#END#]").ToArray();
        string commandNames = string.Empty;
        StringBuilder script = new StringBuilder();

        commandsTextBox.Foreground = Brushes.Black;
        commands.Clear();

        foreach (string l in lines)
        {
            string line = l.Trim('\n', '\r');

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (!string.IsNullOrEmpty(commandNames))
                {
                    foreach (string commandName in commandNames.Split(','))
                    {
                        if (!string.IsNullOrWhiteSpace(commandName) && commands.TryAdd(commandName.Trim(), script.ToString()))
                        {
                            commandsTextBox.Foreground = Brushes.Black;
                        }
                        else
                        {
                            commandsTextBox.Foreground = Brushes.Red;
                            return;
                        }
                    }
                }

                commandNames = line.Trim()[1..^1].Trim();
                script = new StringBuilder();
            }
            else if (!string.IsNullOrEmpty(commandNames))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    _ = script.AppendLine(line);
                }
            }
            else
            {
                commandsTextBox.Foreground = Brushes.Red;
                return;
            }
        }

        File.WriteAllText(cmdPath, commandsTextBox.Text);
    }

    private void FixTextBox_TextChanged(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (prefixTextBox is null || suffixTextBox is null)
        {
            return;
        }

        File.WriteAllLines(fixPath, new[] { prefixTextBox.Text, suffixTextBox.Text });
    }
}