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

        var plumbBuddyDocumentsDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PlumbBuddy");
        if (!Directory.Exists(plumbBuddyDocumentsDirectoryPath))
            Directory.CreateDirectory(plumbBuddyDocumentsDirectoryPath);

        var logPath = Path.Combine(plumbBuddyDocumentsDirectoryPath, "Log.txt");
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog
        (
            new LoggerConfiguration()
#if DEBUG
            .WriteTo.File(Path.Combine(plumbBuddyDocumentsDirectoryPath, "Log.txt"), rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .Enrich.WithAssemblyVersion()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
#else
            .WriteTo.File(Path.Combine(plumbBuddyDocumentsDirectoryPath, "Log.txt"), Serilog.Events.LogEventLevel.Warning, rollingInterval: RollingInterval.Month)
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
            options => options.UseSqlite($"Data Source={Path.Combine(plumbBuddyDocumentsDirectoryPath, "PlumbBuddy.sqlite")}"),
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
#endif

        if (configureMauiAppBuilder is not null)
            configureMauiAppBuilder(builder);

        return builder.Build();
    }
}
