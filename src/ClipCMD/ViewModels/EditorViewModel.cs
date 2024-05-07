using ClipCmd.Utilities;

using System.Collections.ObjectModel;

namespace ClipCmd.ViewModels;

internal class EditorViewModel : ViewModelBase
{
    private readonly ClipCmdCommandHandler clipCmdCommandHandler;

    public new string Title => $"{base.Title} - Editing {Name}";

    public string Name { get; }

    public string Script
    {
        get => clipCmdCommandHandler.Commands[Name].Script;
        set => clipCmdCommandHandler.Commands[Name].Script = value;
    }

    public ObservableCollection<EditorMessageViewModel> Messages { get; } = [];

    public EditorViewModel(string name, ClipCmdCommandHandler clipCmdCommandHandler)
    {
        Name = name;
        this.clipCmdCommandHandler = clipCmdCommandHandler;
    }

    // Only used for the designer
    public EditorViewModel()
    {
        Name = "Test";
        clipCmdCommandHandler = new ClipCmdCommandHandler(new());
    }
}