namespace PlumbBuddy;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates the <see cref="MauiApp"/> to be used in this application
    /// </summary>
    public static MauiApp CreateMauiApp(Action<MauiAppBuilder>? configureMauiAppBuilder = null, Action<ContainerBuilder>? configureContainerBuilder = null)
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureContainer(new AutofacServiceProviderFactory(), configureContainerBuilder);

        builder.Services.AddSingleton(FolderPicker.Default);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog
        (
            new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
#if DEBUG
                .WriteTo.File(Path.Combine(FileSystem.AppDataDirectory, "DebugLog.txt"), rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
                .WriteTo.Debug()
                .Enrich.WithAssemblyVersion()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
#else
                .WriteTo.File(Path.Combine(FileSystem.AppDataDirectory, "Log.txt"), Serilog.Events.LogEventLevel.Information, rollingInterval: RollingInterval.Month, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
                .Enrich.WithAssemblyInformationalVersion()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMemoryUsage()
#endif
                .Enrich.WithAssemblyName()
                .Enrich.WithExceptionData()
                .Enrich.WithExceptionStackTraceHash()
                .Enrich.WithDemystifiedStackTraces()
#if WINDOWS
                .Enrich.WithProperty("HostOperatingSystem", "Windows")
#endif
#if MACCATALYST
                .Enrich.WithProperty("HostOperatingSystem", "macOS")
#endif
                .CreateLogger()
        );

        builder.Services.AddSingleton(Preferences.Default);

        builder.Services.AddDbContext<PbDbContext>
        (
            (serviceProvider, options) => options
                .UseSqlite($"Data Source={Path.Combine(FileSystem.AppDataDirectory, "PlumbBuddy.sqlite")}")
#if DEBUG
                .EnableSensitiveDataLogging()
#endif
                ,
            ServiceLifetime.Transient,
            ServiceLifetime.Transient
        );

        builder.Services.AddPlumbBuddyServices();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

        builder.Services.AddMudMarkdownServices();

        builder.Services.AddMudExtensions();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#else
        if (Preferences.Default.Get(nameof(IPlayer.DevToolsUnlocked), false))
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        if (configureMauiAppBuilder is not null)
            configureMauiAppBuilder(builder);

        return builder.Build();
    }
}
