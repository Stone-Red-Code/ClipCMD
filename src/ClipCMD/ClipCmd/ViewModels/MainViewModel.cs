using ClipCmd.Models;
using ClipCmd.Utilities;

using ReactiveUI;

namespace ClipCmd.ViewModels;

public class MainViewModel : ViewModelBase
{
    public bool AutoTypeModeEnabled
    {
        get => Settings.Current.Mode == ClipCmdMode.AutoType;
        set
        {
            Settings.Current.Mode = value ? ClipCmdMode.AutoType : ClipCmdMode.Clipboard;

            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(ClipboardModeEnabled));
        }
    }

    public bool ClipboardModeEnabled
    {
        get => Settings.Current.Mode == ClipCmdMode.Clipboard;
        set
        {
            Settings.Current.Mode = value ? ClipCmdMode.Clipboard : ClipCmdMode.AutoType;

            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(AutoTypeModeEnabled));
        }
    }

    public int AutoTypeDelay
    {
        get => Settings.Current.AutoTypeDelay;
        set
        {
            Settings.Current.AutoTypeDelay = value;
            this.RaisePropertyChanged();
        }
    }

    public bool AutoPaste
    {
        get => Settings.Current.AutoPaste;
        set
        {
            Settings.Current.AutoPaste = value;
            this.RaisePropertyChanged();
        }
    }

    public string Prefix
    {
        get => Settings.Current.Prefix;
        set
        {
            Settings.Current.Prefix = value;
            this.RaisePropertyChanged();
        }
    }

    public string Suffix
    {
        get => Settings.Current.Suffix;
        set
        {
            Settings.Current.Suffix = value;
            this.RaisePropertyChanged();
        }
    }

    public MainViewModel(ClipCmdCommandHandler clipCmdCommandHandler)
    {
    }

    public MainViewModel()
    {
    }
}