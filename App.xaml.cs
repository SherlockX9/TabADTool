using System.Windows;
using Syncfusion.Licensing;

namespace Text2TreeTool;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        SyncfusionLicenseProvider.RegisterLicense(
            "INSERT YOUR LICENSE KEY HERE");
    }
}
