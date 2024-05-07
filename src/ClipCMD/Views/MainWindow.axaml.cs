using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ClipCmd.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            desktop.MainWindow.Hide();
            e.Cancel = true;
        }
    }
}