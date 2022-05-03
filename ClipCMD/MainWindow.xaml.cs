using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly Dictionary<string, Func<string, string>> stuffToReplace = new()
    {
        { "time", (_) => DateTime.Now.ToShortTimeString() },
        { "date", (_) => DateTime.Now.ToShortDateString() },
        { "calc", (math) => new DataTable().Compute(math.Trim()[4..], null)?.ToString() ?? string.Empty }
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Initialize the clipboard now that we have a window soruce to use
        ClipboardManager windowClipboardManager = new ClipboardManager(this);
        windowClipboardManager.ClipboardChanged += ClipboardManager_ClipboardChanged;

        if (File.Exists("cmd.txt"))
        {
            staticCommandsTextBox.Text = File.ReadAllText("cmd.txt");
        }
        if (File.Exists("fix.txt"))
        {
            string[] lines = File.ReadAllLines("fix.txt");
            if (lines.Length == 2)
            {
                prefixTextBox.Text = lines[0];
                suffixTextBox.Text = lines[1];
            }
            else
            {
                File.Delete("fix.txt");
            }
        }
    }

    private IDataObject? oldData;

    private void ClipboardManager_ClipboardChanged(object? sender, EventArgs e)
    {
        if (!Clipboard.ContainsText() || oldData is not null && Clipboard.IsCurrent(oldData))
        {
            return;
        }

        try
        {
            bool result = SafeRepeat.Start(() => Clipboard.GetText().Trim(), 100, out string? text);

            if (!result || text is null)
            {
                return;
            }

            if (!text.StartsWith(prefixTextBox.Text) || !text.EndsWith(suffixTextBox.Text))
            {
                oldData = Clipboard.GetDataObject();
                return;
            }

            text = text[prefixTextBox.Text.Length..^suffixTextBox.Text.Length].Trim();

            Func<string, string>? func = stuffToReplace.FirstOrDefault(v => text.StartsWith(v.Key)).Value;

            string logText = text;
            string? outText;

            if (func is null)
            {
                string[]? parts = staticCommandsTextBox.Text.Split('\n').FirstOrDefault(l =>
                {
                    if (string.IsNullOrWhiteSpace(l))
                    {
                        return false;
                    }

                    string? startText = l.Trim().Split('>').FirstOrDefault()?.Trim();
                    return startText is not null && text == startText;
                })?.Split('>');

                if (parts?.Length == 2)
                {
                    outText = parts[1];
                }
                else
                {
                    oldData = Clipboard.GetDataObject();
                    return;
                }
            }
            else
            {
                outText = func(text);
            }

            outText = outText.Trim();
            logListBox.Items.Insert(0, $"{logText} > {outText}");

            Clipboard.SetDataObject(outText);

            oldData = Clipboard.GetDataObject();

            InputSimulator inputSimulator = new InputSimulator();
            inputSimulator.Keyboard.KeyPress(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V });
        }
        catch (Exception ex)
        {
            logListBox.Items.Add($"Error: {ex.Message}");
            Debug.WriteLine(ex);
        }
    }

    private void StaticCommandsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool isError = false;
        foreach (string line in staticCommandsTextBox.Text.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line) && line.Count(c => c == '>') != 1)
            {
                isError = true;
            }
        }

        if (isError)
        {
            staticCommandsTextBox.Foreground = Brushes.Red;
        }
        else
        {
            staticCommandsTextBox.Foreground = Brushes.Black;
            File.WriteAllText("cmd.txt", staticCommandsTextBox.Text);
        }
    }

    private void FixTextBox_TextChanged(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (prefixTextBox is null || suffixTextBox is null)
        {
            return;
        }

        File.WriteAllLines("fix.txt", new[] { prefixTextBox.Text, suffixTextBox.Text });
    }
}