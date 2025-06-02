// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Microsoft.Windows.AppLifecycle;
using PlumbBuddy.Platforms.Windows;
using Windows.Win32;

namespace PlumbBuddy.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App :
    MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var keyInstance = AppInstance.FindOrRegisterForKey("PlumbBuddy");
        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += HandleInstanceActivated;
            appLifecycleManager = new(args.Kind);
            InitializeComponent();
        }
        else
        {
            PInvoke.AllowSetForegroundWindow(keyInstance.ProcessId);
            keyInstance.RedirectActivationToAsync(args).GetAwaiter().GetResult();
            Environment.Exit(0);
        }
    }

    readonly AppLifecycleManager? appLifecycleManager;

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() =>
        MauiProgram.CreateMauiApp(ConfigureMauiAppBuilder);

    void ConfigureMauiAppBuilder(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAppLifecycleManager>(appLifecycleManager!);
        builder.Services.AddSingleton<IPlatformFunctions, PlatformFunctions>();
        builder.Services.AddSingleton<IElectronicArtsApp, ElectronicArtsApp>();
        builder.Services.AddSingleton<ISteam, Steam>();
    }

    static void HandleInstanceActivated(object? sender, AppActivationArguments args) =>
        ((App)Current).appLifecycleManager!.ShowWindow();
}
