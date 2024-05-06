using DialogHostAvalonia;

using ReactiveUI;

using System.Windows.Input;

namespace ClipCmd.ViewModels;

internal class InputDialogViewModel(string title, string defaultValue = "") : ViewModelBase
{
    public string Title { get; set; } = title;

    public string Value { get; set; } = defaultValue;

    public ICommand OkCommand => ReactiveCommand.Create(() => DialogHost.GetDialogSession(null)!.Close(Value));

    public ICommand CancelCommand => ReactiveCommand.Create(() => DialogHost.GetDialogSession(null)!.Close(null));
}