using ClipCmd.Models;
using ClipCmd.Utilities;

using DialogHostAvalonia;

using ReactiveUI;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace ClipCmd.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ClipCmdCommandHandler clipCmdCommandHandler;
    private readonly ImportExportService importExportService;

    public List<ClipCmdCommandItemViewModel> Commands => [.. clipCmdCommandHandler.Commands.Keys.Select(x => new ClipCmdCommandItemViewModel(x, clipCmdCommandHandler))];

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

    public int? AutoTypeDelay
    {
        get => Settings.Current.AutoTypeDelay;
        set
        {
            Settings.Current.AutoTypeDelay = value ?? 1;
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

    public string CommandArgsSeperator
    {
        get => Settings.Current.CommandArgsSeperator;
        set
        {
            value = value.Replace(Settings.Current.CommandArgsSeperator, string.Empty);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = Settings.Current.CommandArgsSeperator;
            }

            if (value.Length > 1)
            {
                value = value[..1];
            }

            Settings.Current.CommandArgsSeperator = value;
            this.RaisePropertyChanged();
        }
    }

    public ICommand AddCommand => ReactiveCommand.Create(Add);

    public ICommand ExportCommand => ReactiveCommand.Create(importExportService.Export);
    public ICommand ImportCommand => ReactiveCommand.Create(importExportService.Import);

    public MainViewModel(ClipCmdCommandHandler clipCmdCommandHandler, ImportExportService importExportService)
    {
        this.clipCmdCommandHandler = clipCmdCommandHandler;
        this.importExportService = importExportService;
        clipCmdCommandHandler.Commands.CollectionChanged += (sender, e) =>
        {
            this.RaisePropertyChanged(nameof(Commands));
        };
    }

    // Only used for the designer
    public MainViewModel()
    {
        clipCmdCommandHandler = new ClipCmdCommandHandler(new());
        importExportService = new(new(), clipCmdCommandHandler);
    }

    private async void Add()
    {
        string? name = await DialogHost.Show(new InputDialogViewModel("Enter command name:")) as string;
        name = name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        int counter = 0;
        string originalName = name;

        while (clipCmdCommandHandler.Commands.ContainsKey(name))
        {
            name = $"{originalName} ({++counter})";
        }

        clipCmdCommandHandler.Commands.Add(name, new ClipCmdCommand(@$"Write-Output ""{name} command"""));
        clipCmdCommandHandler.SaveCommands();
    }
}