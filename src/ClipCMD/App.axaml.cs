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
                Settings.SaveSettings();
                clipCmdCommandHandler.SaveCommands();
            };

            clipCmdCommandHandler.Start();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            /*
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = new MainViewModel()
            }
            */

            throw new NotImplementedException();
        }

        base.OnFrameworkInitializationCompleted();
    }
}