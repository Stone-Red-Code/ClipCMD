using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ClipCmd.Models;
using ClipCmd.Utilities;
using ClipCmd.ViewModels;
using ClipCmd.Views;

using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;

using System;

namespace ClipCmd;

public partial class App : Application
{
    public override void Initialize()
    {
        _ = IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            .Register<MaterialDesignIconProvider>();

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            ClipCmdCommandHandler clipCmdCommandHandler = new ClipCmdCommandHandler(desktop.MainWindow);
            desktop.MainWindow.DataContext = new MainViewModel(clipCmdCommandHandler);

            desktop.MainWindow.Closing += (sender, e) =>
            {
                e.Cancel = true;

                desktop.MainWindow.Hide();

                Settings.SaveSettings();
                clipCmdCommandHandler.SaveCommands();
            };

            clipCmdCommandHandler.Start();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            desktop.MainWindow.Show();
            desktop.MainWindow.Activate();
        }
    }

    private void NativeMenuItem_Click_Quit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Settings.SaveSettings();
            desktop.Shutdown();
        }
    }
}