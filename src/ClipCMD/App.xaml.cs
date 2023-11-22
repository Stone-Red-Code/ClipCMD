using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ClipCMD;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string AppName = "ClipCMD";

    public const string AppGuid = "68d780d4-b946-4154-8815-e4632e5ec5d8";

    private Mutex? mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        mutex = new Mutex(true, AppGuid, out bool createdNew);

        if (!createdNew)
        {
            //App is already running! Exiting the application
            _ = MessageBox.Show($"Another instance of {AppName} already running!", $"{AppName} is already running!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Current.Shutdown();
        }

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        Exit += CloseHandler;
    }

    protected void CloseHandler(object sender, EventArgs e)
    {
        mutex?.ReleaseMutex();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        File.WriteAllText("error.log", e.ExceptionObject.ToString());
    }
}