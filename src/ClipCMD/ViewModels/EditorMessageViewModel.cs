using Avalonia.Media;

namespace ClipCmd.ViewModels;

internal class EditorMessageViewModel(string message, IBrush color)
{
    public string Message { get; } = message;

    public IBrush Color { get; } = color;
}