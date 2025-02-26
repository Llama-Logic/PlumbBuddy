namespace PlumbBuddy;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class
/// </summary>
[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public static class MauiProgram
{
    public static DirectoryInfo AppDataDirectory
    {
        get
        {
#if MACCATALYST
            var appDataDirectory = new DirectoryInfo(Path.Combine(FileSystem.AppDataDirectory, "com.llamalogic.plumbbuddy"));
            if (!appDataDirectory.Exists)
                appDataDirectory.Create();
            return appDataDirectory;
#else
            var appDataDirectory = new DirectoryInfo(FileSystem.AppDataDirectory);
            return appDataDirectory;
#endif
        }
    }

    public static DirectoryInfo CacheDirectory
    {
        get
        {
#if MACCATALYST
            var cacheDirectory = new DirectoryInfo(Path.Combine(FileSystem.CacheDirectory, "com.llamalogic.plumbbuddy"));
            if (!cacheDirectory.Exists)
                cacheDirectory.Create();
            return cacheDirectory;
#else
            var cacheDirectory = new DirectoryInfo(FileSystem.CacheDirectory);
            return cacheDirectory;
#endif
        }
    }

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
                .WriteTo.File(Path.Combine(AppDataDirectory.FullName, "DebugLog.txt"), rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
                .WriteTo.Debug()
                .Enrich.WithAssemblyVersion()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
#else
                .WriteTo.File(Path.Combine(AppDataDirectory.FullName, "Log.txt"), Serilog.Events.LogEventLevel.Information, rollingInterval: RollingInterval.Month, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
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

        builder.Services.AddDbContextFactory<PbDbContext>
        (
            (options) => options
                .UseSqlite($"Data Source={Path.Combine(AppDataDirectory.FullName, "PlumbBuddy.sqlite")}", options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
#if DEBUG
                .EnableSensitiveDataLogging()
#endif
        );

        builder.Services.AddLocalization();

        builder.Services.AddPlumbBuddyServices();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddPhorkBlazorReactivity();

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
        if (Preferences.Default.Get(nameof(ISettings.DevToolsUnlocked), false))
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        if (configureMauiAppBuilder is not null)
            configureMauiAppBuilder(builder);

        if (Preferences.Get(nameof(ISettings.VersionAtLastStartup), null) is string versionAtLastStartupStr
            && Version.TryParse(versionAtLastStartupStr, out var versionAtLastStartup))
        {
            var mauiVersion = AppInfo.Version;
            var currentVersion = new Version(mauiVersion.Major, mauiVersion.Minor, mauiVersion.Build);
            if (currentVersion != versionAtLastStartup
                && versionAtLastStartup is { } lastVersion
                && lastVersion is { Major: < 1 } or { Major: 1, Minor: < 3 } or { Major: 1, Minor: 3, Build: < 8 })
            {
                var mdcDatabase = new FileInfo(Path.Combine(AppDataDirectory.FullName, "PlumbBuddy.sqlite"));
                if (mdcDatabase.Exists)
                    mdcDatabase.Delete();
                var mdcDatabaseSharedMemory = new FileInfo(Path.Combine(AppDataDirectory.FullName, "PlumbBuddy.sqlite-shm"));
                if (mdcDatabaseSharedMemory.Exists)
                    mdcDatabaseSharedMemory.Delete();
                var mdcDatabaseWriteAheadLog = new FileInfo(Path.Combine(AppDataDirectory.FullName, "PlumbBuddy.sqlite-wal"));
                if (mdcDatabaseWriteAheadLog.Exists)
                    mdcDatabaseWriteAheadLog.Delete();
            }
        }

        return builder.Build();
    }
}
