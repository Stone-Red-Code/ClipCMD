using ReactiveUI;

using System.Diagnostics;
using System.Reflection;

namespace ClipCmd.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public string Title
    {
        get
        {
            string title = "ClipCMD";
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string? version = fileVersionInfo.ProductVersion;

#if DEBUG
            return $"{title} - Dev {version}";
#else
            return $"{title} - {version}";
#endif
        }
    }
}