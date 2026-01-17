// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
using Microsoft.Windows.AppLifecycle;
using PlumbBuddy.Platforms.Windows;
using PlumbBuddy.Platforms.Windows.Input;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PlumbBuddy.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App :
    MauiWinUIApplication
{
    const string publisherHash = "27xcxqx0sss1r";

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        var appxPackagesDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages"));
        if (appxPackagesDirectory.Exists
            && appxPackagesDirectory.GetDirectories().Count(packageDirectory => packageDirectory.Name.StartsWith("com.llamalogic.plumbbuddy_", StringComparison.OrdinalIgnoreCase)) is > 1)
        {
            var processId = Environment.ProcessId;
            foreach (var process in Process.GetProcesses()
                .Select(p =>
                {
                    try
                    {
                        return (process: p, mainModule: p.MainModule!);
                    }
                    catch (Win32Exception)
                    {
                        return (process: p, mainModule: null!);
                    }
                })
                .Where(t => t.mainModule is not null)
                .Select(t => (t.process, file: new FileInfo(t.mainModule.FileName)))
                .Where(t => t.file.Name == "PlumbBuddy.exe" && t.process.Id != processId)
                .Select(t => t.process))
                process.Kill();
            if (PInvoke.MessageBox
            (
                new(IntPtr.Zero),
                $"""
                You have more than one PlumbBuddy installation at the moment, and you must only have one.
                
                This is definitely not your fault. Sometimes, when we need to update part of how we build and digitally sign PlumbBuddy for your protection, we change something which causes Windows to think of it as an entirely new app. However, PlumbBuddy is not designed to work properly with sibling installations of itself.
                
                We'd like to launch Windows Settings for you now so that you can type PlumbBuddy into the "Search apps" box and then remove all but the most recent installation. Windows will show you the dates you installed them. Remove each one BUT the most recent one.

                Press OK to launch Windows Settings and exit PlumbBuddy.
                Press CANCEL to exit PlumbBuddy.
                """,
                "Multiple Installations of PlumbBuddy Detected",
                MESSAGEBOX_STYLE.MB_ICONSTOP | MESSAGEBOX_STYLE.MB_OKCANCEL
            ) is MESSAGEBOX_RESULT.IDOK)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:appsfeatures",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    PInvoke.MessageBox
                    (
                        new(IntPtr.Zero),
                        $"""
                        I tried to launch Settings and open Installed apps for you, but it didn't work.

                        Technical details:
                        {ex.GetType().Name}: {ex.Message}
                        """,
                        "Whoops!",
                        MESSAGEBOX_STYLE.MB_ICONERROR | MESSAGEBOX_STYLE.MB_OK
                    );
                }
            }
            var packagesDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages"));
            var thisPackageDirectory = new DirectoryInfo(Path.Combine(packagesDirectory.FullName, $"com.llamalogic.plumbbuddy_{publisherHash}"));
            if (thisPackageDirectory.Exists)
            {
                foreach (var otherPackageDirectory in packagesDirectory
                    .GetDirectories()
                    .Where(d => d.Name.StartsWith("com.llamalogic.plumbbuddy_", StringComparison.OrdinalIgnoreCase) && !d.Name.EndsWith($"_{publisherHash}", StringComparison.OrdinalIgnoreCase)))
                {
                    var settingsFile = new FileInfo(Path.Combine(otherPackageDirectory.FullName, "Settings", "settings.dat"));
                    if (settingsFile.Exists)
                        settingsFile.CopyTo(Path.Combine(thisPackageDirectory.FullName, "Settings", "settings.dat"), true);
                    var fromLocalState = new DirectoryInfo(Path.Combine(otherPackageDirectory.FullName, "LocalState"));
                    if (fromLocalState.Exists)
                    {
                        IReadOnlyList<string> extensions = [".sqlite", ".sqlite-wal", ".sqlite-shm", ".txt"];
                        var toLocalState = Directory.CreateDirectory(Path.Combine(thisPackageDirectory.FullName, "LocalState"));
                        foreach (var file in fromLocalState.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                            .Where(file => extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
                            file.CopyTo(Path.Combine(toLocalState.FullName, file.Name), true);
                        var fromRDS = new DirectoryInfo(Path.Combine(fromLocalState.FullName, "Relational Data Storage"));
                        if (fromRDS.Exists)
                        {
                            var toRDS = Directory.CreateDirectory(Path.Combine(toLocalState.FullName, "Relational Data Storage"));
                            foreach (var file in fromRDS.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                .Where(file => extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
                                file.CopyTo(Path.Combine(toRDS.FullName, file.Name), true);
                        }
                    }
                    break;
                }
            }
            Environment.Exit(0);
            return;
        }
        var args = AppInstance.GetCurrent().GetActivatedEventArgs();
        var keyInstance = AppInstance.FindOrRegisterForKey("PlumbBuddy");
        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += HandleInstanceActivated;
            appLifecycleManager = new(this, args.Kind);
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
        builder.Services.AddSingleton<IElectronicArtsApp, Platforms.Windows.ElectronicArtsApp>();
        builder.Services.AddSingleton<ISteam, Steam>();
        builder.Services.AddSingleton<IDesktopInputInterceptor, DesktopInputInterceptor>();
        builder.Services.AddSingleton<IGamepadInterop, GamepadInterop>();
    }

    static void HandleInstanceActivated(object? sender, AppActivationArguments args) =>
        ((App)Current).appLifecycleManager!.ShowWindow();
}
