using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ClipCMD;

public class RuntimeData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool autoTypeRunning = false;
    private string commandsInfo = string.Empty;
    private string commandsText = string.Empty;
    private Visibility errorPanelVisible = Visibility.Collapsed;
    private Visibility logPanelVisible = Visibility.Visible;

    public bool AutoTypeRunning
    {
        get => autoTypeRunning;
        set
        {
            autoTypeRunning = value;
            OnPropertyChanged();
        }
    }

    public string CommandsInfo
    {
        get => commandsInfo;
        set
        {
            commandsInfo = value;
            OnPropertyChanged();
        }
    }

    public string CommandsText
    {
        get => commandsText;
        set
        {
            commandsText = value;
            OnPropertyChanged();
        }
    }

    public Brush CommandsTextColor => Errors.Count > 0 ? Brushes.Red : Brushes.Black;

    public Visibility ErrorPanelVisible
    {
        get => errorPanelVisible;
        set
        {
            errorPanelVisible = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Errors { get; } = new();

    public Visibility LogPanelVisible
    {
        get => logPanelVisible;
        set
        {
            logPanelVisible = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Logs { get; } = new();

    public RuntimeData()
    {
        Errors.CollectionChanged += (sender, args) =>
        {
            ErrorPanelVisible = Errors.Any() ? Visibility.Visible : Visibility.Collapsed;
            LogPanelVisible = Errors.Any() ? Visibility.Collapsed : Visibility.Visible;
            OnPropertyChanged(nameof(CommandsTextColor));
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}