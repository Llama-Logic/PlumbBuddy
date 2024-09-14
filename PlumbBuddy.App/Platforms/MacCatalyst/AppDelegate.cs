using Foundation;

namespace PlumbBuddy.App.Platforms.MacCatalyst;

[Register("AppDelegate")]
public class AppDelegate :
    MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() =>
        MauiProgram.CreateMauiApp(ConfigureMauiAppBuilder);

    void ConfigureMauiAppBuilder(MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IAppLifecycleManager>(appLifecycleManager!);
        builder.Services.AddSingleton<IPlatformFunctions, PlatformFunctions>();
        builder.Services.AddSingleton<IElectronicArtsApp, ElectronicArtsApp>();
        builder.Services.AddSingleton<ISteam, Steam>();
    }
}
