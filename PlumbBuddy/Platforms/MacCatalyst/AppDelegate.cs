using Foundation;

namespace PlumbBuddy.Platforms.MacCatalyst;

[Register("AppDelegate")]
public class AppDelegate :
    MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() =>
        MauiProgram.CreateMauiApp(ConfigureMauiAppBuilder);

    void ConfigureMauiAppBuilder(MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<UiBridgeWebView, UiBridgeWebViewHandler>());
        builder.Services.AddSingleton<IAppLifecycleManager>(Program.AppLifecycleManager);
        builder.Services.AddSingleton<IPlatformFunctions, PlatformFunctions>();
        builder.Services.AddSingleton<IElectronicArtsApp, Platforms.MacCatalyst.ElectronicArtsApp>();
        builder.Services.AddSingleton<ISteam, Steam>();
    }
}
