using Microsoft.VisualBasic.FileIO;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Windows;

using WindowsInput;
using WindowsInput.Native;

namespace ClipCMD;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Dictionary<string, string> commands = new Dictionary<string, string>();

    private readonly System.Windows.Forms.NotifyIcon notifyIcon;
    private WindowState storedWindowState = WindowState.Normal;

    private readonly string cmdPath;
    private readonly string fixPath;

    public MainWindow()
    {
        notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            BalloonTipText = "ClipCMD has been minimized. Click the tray icon to show.",
            BalloonTipTitle = "ClipCMD",
            Text = "ClipCMD",
            Icon = new System.Drawing.Icon("logo.ico"),
            Visible = true
        };

        notifyIcon.Click += NotifyIcon_Click;

        InitializeComponent();

        string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(applicationDataPath, "ClipCMD");

        if (!Directory.Exists(folderPath))
        {
            _ = Directory.CreateDirectory(folderPath);
        }

        cmdPath = Path.Combine(folderPath, "cmd.txt");
        fixPath = Path.Combine(folderPath, "fix.txt");
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

    private void OnClose(object? sender, CancelEventArgs args)
    {
        notifyIcon.Dispose();
    }

    private void OnStateChanged(object? sender, EventArgs args)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            notifyIcon?.ShowBalloonTip(2000);
        }
        else
        {
            storedWindowState = WindowState;
        }
    }

    private void NotifyIcon_Click(object? sender, EventArgs e)
    {
        Show();
        WindowState = storedWindowState;
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

            clipboardText = clipboardText[prefixTextBox.Text.Length..^suffixTextBox.Text.Length].Trim().Replace("\"\"", "\0");

            TextFieldParser parser = new TextFieldParser(new StringReader(clipboardText))
            {
                HasFieldsEnclosedInQuotes = true
            };

            parser.SetDelimiters(" ");

            string[] sections = parser.ReadFields() ?? Array.Empty<string>();

            sections = sections.Select(s => s.Replace('\0', '\"')).ToArray();

            if (sections.Length == 0)
            {
                return;
            }

            string command = sections[0].Trim();

            if (!commands.TryGetValue(command, out string? script))
            {
                oldData = Clipboard.GetDataObject();
                return;
            }

            PowerShell ps = PowerShell
                .Create()
                .AddScript(script)
                .AddParameters(sections.Skip(1).ToList());

            StringBuilder outText = new StringBuilder();

            foreach (PSObject commandResult in ps.Invoke())
            {
                _ = outText.AppendLine(commandResult?.ToString() ?? string.Empty);
            }

            logListBox.Items.Insert(0, $"{command} > {outText.ToString().TrimEnd()}");

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
        string[] lines = commandsTextBox.Text.Split('\n').Append("[#END#]").ToArray();
        string commandNames = string.Empty;
        StringBuilder script = new StringBuilder();

        commandsTextBox.Foreground = System.Windows.Media.Brushes.Black;
        errorPanel.Visibility = Visibility.Collapsed;
        logPanel.Visibility = Visibility.Visible;
        errorListBox.Items.Clear();
        commands.Clear();

        foreach (string l in lines)
        {
            string line = l.Trim('\n', '\r');

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (!string.IsNullOrEmpty(commandNames))
                {
                    _ = Parser.ParseInput(script.ToString(), out _, out ParseError[] errors);

                    if (errors.Length > 0)
                    {
                        foreach (ParseError error in errors)
                        {
                            AddError(commandNames, error.ToString());
                        }
                    }

                    foreach (string commandName in commandNames.Split(','))
                    {
                        if (string.IsNullOrWhiteSpace(commandName))
                        {
                            AddError(commandNames, "Command \"{commandName}\" is empty!");
                        }
                        else if (!commands.TryAdd(commandName.Trim(), script.ToString()))
                        {
                            AddError(commandNames, "Command \"{commandName}\" already exists!");
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
                AddError(commandNames, "Input has to start with [<Command Name>]!");
                return;
            }
        }

        _ = commands.TryAdd("list", $"\"{string.Join(", ", commands.Keys)}\"");

        File.WriteAllText(cmdPath, commandsTextBox.Text);
    }

    private void AddError(string commandNames, string error)
    {
        errorPanel.Visibility = Visibility.Visible;
        logPanel.Visibility = Visibility.Collapsed;
        commandsTextBox.Foreground = System.Windows.Media.Brushes.Red;
        _ = errorListBox.Items.Add($"[{commandNames}]\n{error}");
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