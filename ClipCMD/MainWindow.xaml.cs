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
using System.Text.Json;
using System.Threading.Tasks;
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
    private readonly string commandsPath;
    private readonly System.Windows.Forms.NotifyIcon notifyIcon;
    private readonly string settingsPath;
    private IDataObject? oldData;
    private WindowState storedWindowState = WindowState.Normal;
    public RuntimeData RuntimeData { get; set; } = new();
    public Settings Settings { get; set; } = new();

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

        string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(applicationDataPath, "ClipCMD");

        if (!Directory.Exists(folderPath))
        {
            _ = Directory.CreateDirectory(folderPath);
        }

        commandsPath = Path.Combine(folderPath, "cmd.txt");
        settingsPath = Path.Combine(folderPath, "settings.txt");

        if (File.Exists(commandsPath))
        {
            RuntimeData.CommandsText = File.ReadAllText(commandsPath);
        }

        if (File.Exists(settingsPath))
        {
            Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath)) ?? new Settings();
        }

        DataContext = this;

        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        // Initialize the clipboard now that we have a window source to use
        ClipboardManager windowClipboardManager = new ClipboardManager(this);
        windowClipboardManager.ClipboardChanged += ClipboardManager_ClipboardChanged;
    }

    private void AddError(string commandNames, string error)
    {
        RuntimeData.Errors.Add($"[{commandNames}]\n{error}");
    }

    private void CancelAutoTypeButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimeData.AutoTypeRunning = false;
    }

    private async void ClipboardManager_ClipboardChanged(object? sender, EventArgs e)
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

            if (!clipboardText.StartsWith(Settings.Prefix) || !clipboardText.EndsWith(Settings.Suffix))
            {
                oldData = Clipboard.GetDataObject();
                return;
            }

            clipboardText = clipboardText[Settings.Prefix.Length..^Settings.Suffix.Length].Trim().Replace("\"\"", "\0");

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

            RuntimeData.Logs.Insert(0, $"{command} > {outText.ToString().TrimEnd()}");

            InputSimulator inputSimulator = new InputSimulator();

            oldData = Clipboard.GetDataObject();

            if (Settings.Mode == ClipCMDMode.ClipBoard)
            {
                Clipboard.SetText(outText.ToString().TrimEnd());

                if (Settings.AutoPaste)
                {
                    _ = inputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL }, new[] { VirtualKeyCode.VK_V });
                }
            }
            else if (Settings.Mode == ClipCMDMode.AutoType)
            {
                RuntimeData.AutoTypeRunning = true;

                foreach (char c in outText.ToString())
                {
                    _ = inputSimulator.Keyboard.TextEntry(c);
                    await Task.Delay(Settings.AutoTypeDelay);
                    if (!RuntimeData.AutoTypeRunning)
                    {
                        break;
                    }
                }

                RuntimeData.AutoTypeRunning = false;
            }
        }
        catch (Exception ex)
        {
            RuntimeData.Logs.Insert(0, $"Error: {ex.Message}");
            Debug.WriteLine(ex);
        }
    }

    private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(RuntimeData.CommandsText);
        _ = MessageBox.Show($"Successfully copied {commands.Count - 1} command(s) to clipboard!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NotifyIcon_Click(object? sender, EventArgs e)
    {
        Show();
        WindowState = storedWindowState;
    }

    private void OnClose(object? sender, CancelEventArgs args)
    {
        notifyIcon.Dispose();
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(Settings));
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

    private void StaticCommandsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string[] lines = RuntimeData.CommandsText.Split('\n').Append("[#END#]").ToArray();
        string commandNames = string.Empty;
        StringBuilder script = new StringBuilder();

        RuntimeData.Errors.Clear();
        commands.Clear();

        RuntimeData.CommandsInfo = $"Lines: {lines.Length}    Commands: {commands.Count}    Errors: {RuntimeData.Errors.Count}";

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
                            AddError(commandNames, $"Command \"{commandName}\" is empty!");
                        }
                        else if (commandName.Any(char.IsWhiteSpace))
                        {
                            AddError(commandNames, $"Command \"{commandName}\" can't contain white spaces!");
                        }
                        else if (!commands.TryAdd(commandName.Trim(), script.ToString()))
                        {
                            AddError(commandNames, $"Command \"{commandName}\" already exists!");
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

        RuntimeData.CommandsInfo = $"Lines: {lines.Length}    Commands: {commands.Count}    Errors: {RuntimeData.Errors.Count}";

        _ = commands.TryAdd("list", $"\"{string.Join(", ", commands.Keys)}\"");

        File.WriteAllText(commandsPath, RuntimeData.CommandsText);
    }
}