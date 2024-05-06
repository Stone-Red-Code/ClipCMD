using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using ClipCmd.Models;
using ClipCmd.Utilities;
using ClipCmd.Views;

using DialogHostAvalonia;

using ReactiveUI;

using System.Collections.Generic;
using System.Windows.Input;

namespace ClipCmd.ViewModels;

public class ClipCmdCommandItemViewModel(string name, ClipCmdCommandHandler clipCmdCommandHandler)
{
    public string Name { get; } = name;

    public ICommand RemoveCommand => ReactiveCommand.Create(Remove);

    public ICommand RenameCommand => ReactiveCommand.Create(Rename);

    public ICommand EditCommand => ReactiveCommand.Create(Edit);

    private void Remove()
    {
        _ = clipCmdCommandHandler.Commands.Remove(Name);
        clipCmdCommandHandler.SaveCommands();
    }

    private async void Rename()
    {
        string? newName = await DialogHost.Show(new InputDialogViewModel("Edit command name:", Name)) as string;
        newName = newName?.Trim();

        if (!clipCmdCommandHandler.Commands.Remove(Name, out ClipCmdCommand? clipCmdCommand))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            clipCmdCommandHandler.Commands.Add(Name, clipCmdCommand);
            return;
        }

        int counter = 0;
        string originalName = newName;

        while (clipCmdCommandHandler.Commands.ContainsKey(newName))
        {
            newName = $"{originalName} ({++counter})";
        }

        clipCmdCommandHandler.Commands.Add(newName, clipCmdCommand);
        clipCmdCommandHandler.SaveCommands();
    }

    private async void Edit()
    {
        Avalonia.Controls.Window? mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

        EditorWindow editorWindow = new EditorWindow
        {
            DataContext = new EditorViewModel(Name, clipCmdCommandHandler)
        };

        await editorWindow.ShowDialog(mainWindow!);
        clipCmdCommandHandler.SaveCommands();
    }
}