using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipCMD;

public class Settings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool autoPaste = true;
    private int autoTypeDelay = 100;
    private ClipCMDMode mode = ClipCMDMode.ClipBoard;
    private string prefix = "_";
    private string suffix = "_";

    public bool AutoPaste
    {
        get => autoPaste;
        set
        {
            autoPaste = value;
            OnPropertyChanged();
        }
    }

    public int AutoTypeDelay
    {
        get => autoTypeDelay;
        set
        {
            autoTypeDelay = Math.Clamp(value, 0, 1000);
            OnPropertyChanged();
        }
    }

    public ClipCMDMode Mode
    {
        get => mode;
        set
        {
            mode = value;
            OnPropertyChanged();
        }
    }

    public string Prefix
    {
        get => prefix;
        set
        {
            prefix = value;
            OnPropertyChanged();
        }
    }

    public string Suffix
    {
        get => suffix;
        set
        {
            suffix = value;
            OnPropertyChanged();
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public enum ClipCMDMode
{
    ClipBoard,
    AutoType
}