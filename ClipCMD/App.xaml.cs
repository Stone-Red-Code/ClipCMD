using System.Threading;
using System.Windows;

namespace ClipCMD;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    Mutex mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "ClipCMD";

        mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            //App is already running! Exiting the application
            _ = MessageBox.Show($"Another instance of {appName} already running!", $"{appName} is already running!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Current.Shutdown();
        }

        base.OnStartup(e);
    }
}