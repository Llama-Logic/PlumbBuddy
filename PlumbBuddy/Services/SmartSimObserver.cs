namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class SmartSimObserver :
    ISmartSimObserver
{
    static readonly Regex modsDirectoryRelativePathPattern = GetModsDirectoryRelativePathPattern();
    static readonly Regex trimmedLocalPathSegmentsPattern = GetTrimmedLocalPathSegmentsPattern();

    public const string IntegrationPackageName = "PlumbBuddy_Integration.package";
    public const string IntegrationScriptModName = "PlumbBuddy_Integration.ts4script";

    static ModsDirectoryFileType CatalogIfLikelyErrorOrTraceLogInRoot(string userDataDirectoryRelativePath)
    {
        if (userDataDirectoryRelativePath.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return ModsDirectoryFileType.Ignored;
        if (!userDataDirectoryRelativePath.Contains("exception", StringComparison.OrdinalIgnoreCase)
            && !userDataDirectoryRelativePath.Contains("crash", StringComparison.OrdinalIgnoreCase))
            return ModsDirectoryFileType.Ignored;
        if (userDataDirectoryRelativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            || userDataDirectoryRelativePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            return ModsDirectoryFileType.TextFile;
        if (userDataDirectoryRelativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || userDataDirectoryRelativePath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
            return ModsDirectoryFileType.HtmlFile;
        return ModsDirectoryFileType.Ignored;
    }

    [GeneratedRegex(@"\-disablepacks:(?<packCodes>[^\s]*)")]
    private static partial Regex GetDisablePacksCommandLineArgumentPattern();

    [GeneratedRegex(@"^Mods[\\/].+$")]
    private static partial Regex GetModsDirectoryRelativePathPattern();

    [GeneratedRegex(@"^(?<path>.*?)[\\/]?$")]
    private static partial Regex GetTrimmedLocalPathSegmentsPattern();

    [GeneratedRegex(@"^\wp\d{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetTs4PackCodePattern();

    public SmartSimObserver(ILifetimeScope lifetimeScope, ILogger<ISmartSimObserver> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, IAppLifecycleManager appLifecycleManager, ISettings settings, IGameResourceCataloger gameResourceCataloger, IModsDirectoryCataloger modsDirectoryCataloger, IElectronicArtsApp electronicArtsApp, ISteam steam, ISuperSnacks superSnacks, IBlazorFramework blazorFramework, IPublicCatalogs publicCatalogs, IProxyHost proxyHost)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(gameResourceCataloger);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(electronicArtsApp);
        ArgumentNullException.ThrowIfNull(steam);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        ArgumentNullException.ThrowIfNull(proxyHost);
        this.lifetimeScope = lifetimeScope.BeginLifetimeScope(ConfigureLifetimeScope);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.appLifecycleManager = appLifecycleManager;
        this.settings = settings;
        this.gameResourceCataloger = gameResourceCataloger;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.electronicArtsApp = electronicArtsApp;
        this.steam = steam;
        this.superSnacks = superSnacks;
        this.blazorFramework = blazorFramework;
        this.publicCatalogs = publicCatalogs;
        this.platformFunctions.PerformanceProcessorAffinity.ToString();
        this.proxyHost = proxyHost;
        enqueuedScanningTaskLock = new();
        enqueuedResamplingPacksTaskLock = new();
        enqueuedFresheningTaskLock = new();
        fileSystemWatcherConnectionLock = new();
        fresheningTaskLock = new();
        gameProcessOptimizationLock = new();
        resampleGameVersionDebouncer = new(ResampleGameVersionAsync, TimeSpan.FromSeconds(3));
        resamplingPacksTaskLock = new();
        scanInstances = [];
        scanIssues = [];
        scanningTaskLock = new();
        fileSystemStringComparison = platformFunctions.FileSystemStringComparison;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        ConnectToInstallationDirectory();
        ConnectToUserDataDirectory();
    }

    ~SmartSimObserver() =>
        Dispose(false);

    readonly IAppLifecycleManager appLifecycleManager;
    readonly IBlazorFramework blazorFramework;
    ImmutableArray<FileSystemInfo> cacheComponents;
    IReadOnlyList<string> disabledPackCodes = [];
    readonly IElectronicArtsApp electronicArtsApp;
    readonly AsyncLock enqueuedFresheningTaskLock;
    readonly AsyncLock enqueuedResamplingPacksTaskLock;
    readonly AsyncLock enqueuedScanningTaskLock;
    readonly StringComparison fileSystemStringComparison;
    readonly AsyncLock fileSystemWatcherConnectionLock;
    readonly AsyncLock fresheningTaskLock;
    readonly AsyncLock gameProcessOptimizationLock;
    Version? gameVersion;
    readonly IGameResourceCataloger gameResourceCataloger;
    ImmutableArray<byte> integrationPackageLastSha256 = ImmutableArray<byte>.Empty;
    ImmutableArray<byte> integrationScriptModLastSha256 = ImmutableArray<byte>.Empty;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? installationDirectoryWatcher;
    IReadOnlyList<string> installedPackCodes = [];
    bool isPerformanceProcessorAffinityInEffect;
    bool isScanning;
    bool isModsDisabledGameSettingOn;
    bool isScriptModsEnabledGameSettingOn = true;
    bool isShowModListStartupGameSettingOn;
    bool isSteamInstallation;
    string lastModHealthStatusSummary = string.Empty;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? launcherUserDataDirectoryWatcher;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<ISmartSimObserver> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? packsDirectoryWatcher;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly IProxyHost proxyHost;
    readonly IPublicCatalogs publicCatalogs;
    readonly ISettings settings;
    readonly AsyncDebouncer resampleGameVersionDebouncer;
    readonly AsyncLock resamplingPacksTaskLock;
    IReadOnlyList<ScanIssue> scanIssues;
    readonly ConcurrentDictionary<Type, IScan> scanInstances;
    readonly AsyncLock scanningTaskLock;
    readonly ISteam steam;
    readonly ISuperSnacks superSnacks;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? userDataDirectoryWatcher;

    public IReadOnlyList<string> DisabledPackCodes
    {
        get => disabledPackCodes;
        private set
        {
            var cleaned = value
                .Where(code => GetTs4PackCodePattern().IsMatch(code))
                .Select(code => code.Trim().ToUpperInvariant())
                .Distinct()
                .Order();
            if (disabledPackCodes.SequenceEqual(cleaned))
                return;
            disabledPackCodes = [..cleaned];
            FreshenIntegration(force: true);
            OnPropertyChanged();
            if (ScanIssues.Count is > 0)
                Scan();
        }
    }

    public Version? GameVersion
    {
        get => gameVersion;
        private set
        {
            if (EqualityComparer<Version>.Default.Equals(gameVersion, value))
                return;
            gameVersion = value;
            if (settings.LastGameVersion is { } lastGameVersion
                && gameVersion is not null)
            {
                var truncatedLastGameVersion = new Version(lastGameVersion.Major, lastGameVersion.Minor);
                var truncatedGameVersion = new Version(gameVersion.Major, gameVersion.Minor);
                if (!EqualityComparer<Version>.Default.Equals(truncatedLastGameVersion, truncatedGameVersion) && settings.OfferPatchDayModUpdatesHelp)
                {
                    using var pbDbContext = pbDbContextFactory.CreateDbContext();
                    if (!IsModsDisabledGameSettingOn
                        && pbDbContext.ModFiles.Any(mf => mf.FileType == ModsDirectoryFileType.Package)
                        || IsScriptModsEnabledGameSettingOn
                        && pbDbContext.ModFiles.Any(mf => mf.FileType == ModsDirectoryFileType.ScriptArchive))
                    {
                        superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.SmartSimObserver_Success_OfferPatchDayModUpdatesHelp, truncatedLastGameVersion, truncatedGameVersion)), Severity.Success, options =>
                        {
                            options.Icon = MaterialDesignIcons.Normal.TimelineCheck;
                            options.RequireInteraction = true;
                            var lifetimeScope = blazorFramework.MainLayoutLifetimeScope!;
                            var dialogService = lifetimeScope.Resolve<IDialogService>();
                            var publicCatalogs = lifetimeScope.Resolve<IPublicCatalogs>();
                            options.OnClick = async _ => await dialogService.ShowAskForHelpDialogAsync(logger, publicCatalogs, isPatchDay: true);
                        });
                        _ = Task.Run(async () => await platformFunctions.SendLocalNotificationAsync(AppText.SmartSimObserver_Notification_OfferPatchDayModUpdatesHelp_Caption, AppText.SmartSimObserver_Notification_OfferPatchDayModUpdatesHelp_Text));
                    }
                }
            }
            settings.LastGameVersion = gameVersion;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> InstalledPackCodes
    {
        get => installedPackCodes;
        private set
        {
            var cleaned = value
                .Where(code => GetTs4PackCodePattern().IsMatch(code))
                .Select(code => code.Trim().ToUpperInvariant())
                .Distinct()
                .Order();
            if (installedPackCodes.SequenceEqual(cleaned))
                return;
            installedPackCodes = [..cleaned];
            FreshenIntegration(force: true);
            OnPropertyChanged();
            if (ScanIssues.Count is > 0)
                Scan();
        }
    }

    public bool IsPerformanceProcessorAffinityInEffect
    {
        get => isPerformanceProcessorAffinityInEffect;
        private set
        {
            if (isPerformanceProcessorAffinityInEffect == value)
                return;
            isPerformanceProcessorAffinityInEffect = value;
            OnPropertyChanged();
        }
    }

    public bool IsScanning
    {
        get => isScanning;
        private set
        {
            if (isScanning == value)
                return;
            isScanning = value;
            OnPropertyChanged();
        }
    }

    public bool IsModsDisabledGameSettingOn
    {
        get => isModsDisabledGameSettingOn;
        private set
        {
            if (isModsDisabledGameSettingOn == value)
                return;
            isModsDisabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsScriptModsEnabledGameSettingOn
    {
        get => isScriptModsEnabledGameSettingOn;
        private set
        {
            if (isScriptModsEnabledGameSettingOn == value)
                return;
            isScriptModsEnabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsShowModListStartupGameSettingOn
    {
        get => isShowModListStartupGameSettingOn;
        private set
        {
            if (isShowModListStartupGameSettingOn == value)
                return;
            isShowModListStartupGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsSteamInstallation
    {
        get => isSteamInstallation;
        private set
        {
            if (isSteamInstallation == value)
                return;
            isSteamInstallation = value;
            OnPropertyChanged();
        }
    }

    string PacksDirectoryPath =>
#if MACCATALYST
        Path.Combine(new DirectoryInfo(settings.InstallationFolderPath).Parent!.FullName, "The Sims 4 Packs");
#else
        settings.InstallationFolderPath;
#endif

    public IReadOnlyList<ScanIssue> ScanIssues
    {
        get => scanIssues;
        private set
        {
            scanIssues = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void ApplyGameProcessEnhancements() =>
        _ = Task.Run(() => ApplyGameProcessEnhancementsAsync(null));

    async Task ApplyGameProcessEnhancementsAsync(Process? ts4Process)
    {
        if (!platformFunctions.IsGameProcessOptimizationSupported)
            return;
        if (ts4Process is null)
        {
            ts4Process = await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false);
            if (ts4Process is null)
                return;
        }
        using var gameProcessOptimizationLockHeld = await gameProcessOptimizationLock.LockAsync().ConfigureAwait(false);
        if (platformFunctions.ProcessorsHavePerformanceVarianceAndConfigurableAffinity)
        {
            var arePCoresOnlyRequested = settings.Type is not UserType.Casual
                && settings.ForceGameProcessPerformanceProcessorAffinity;
            var requestedAffinity = arePCoresOnlyRequested
                ? platformFunctions.PerformanceProcessorAffinity
                : platformFunctions.DefaultProcessorAffinity;
            if (requestedAffinity is not 0)
            {
                ts4Process.ProcessorAffinity = requestedAffinity;
                IsPerformanceProcessorAffinityInEffect = arePCoresOnlyRequested;
            }
        }
    }

    bool CatalogIfInModsDirectory(string userDataDirectoryRelativePath, out bool wasIntegrationChange)
    {
        if (modsDirectoryRelativePathPattern.IsMatch(userDataDirectoryRelativePath))
        {
            NoticeIfGameIsRunning();
            var modsDirectoryRelativePath = userDataDirectoryRelativePath[5..];
            wasIntegrationChange = modsDirectoryRelativePath is IntegrationPackageName or IntegrationScriptModName;
            if (!wasIntegrationChange)
                modsDirectoryCataloger.Catalog(modsDirectoryRelativePath);
            return !wasIntegrationChange;
        }
        wasIntegrationChange = false;
        return false;
    }

    bool CatalogIfModsDirectory(string userDataDirectoryRelativePath)
    {
        if (userDataDirectoryRelativePath == "Mods")
        {
            NoticeIfGameIsRunning();
            modsDirectoryCataloger.Catalog(string.Empty);
            return true;
        }
        return false;
    }

    void CheckIfSteam() =>
        _ = Task.Run(CheckIfSteamAsync);

    async Task CheckIfSteamAsync()
    {
        IsSteamInstallation =
            await steam.GetTS4InstallationDirectoryAsync() is { } steamInstallationDirectory
            && Path.GetFullPath(steamInstallationDirectory.FullName) == Path.GetFullPath(settings.InstallationFolderPath);
        await ResampleDisabledPacksAsync().ConfigureAwait(false);
        await ResetLauncherUserDataMonitoringAsync().ConfigureAwait(false);
    }

    public async Task<bool> ClearCacheAsync()
    {
        try
        {
            var saveScratchDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves", "scratch"));
            if (saveScratchDirectory.Exists)
                saveScratchDirectory.Delete(true);
            foreach (var cacheComponent in cacheComponents)
            {
                var retries = 20;
                while (true)
                {
                    try
                    {
                        cacheComponent.Refresh();
                        if (cacheComponent.Exists)
                        {
                            if (cacheComponent is DirectoryInfo directoryCacheComponent)
                            {
                                foreach (var cacheSubComponent in directoryCacheComponent.GetFiles("*.*", SearchOption.AllDirectories))
                                    cacheSubComponent.Delete();
                            }
                            else
                                cacheComponent.Delete();
                        }
                        break;
                    }
                    catch (UnauthorizedAccessException) when (--retries >= 0)
                    {
                        await Task.Delay(250).ConfigureAwait(false);
                    }
                }
            }
            ResampleCacheClarity();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "a user-initiated attempt to clear the cache failed");
            superSnacks.OfferRefreshments(new MarkupString(
                string.Format(AppText.SmartSimObserver_Error_ClearingCacheFailed, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.EraserVariant;
                    options.OnClick = async _ =>
                    {
                        var dialogService = blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>();
                        if (await dialogService.GetSupportDiscordAsync(logger, publicCatalogs, "PlumbBuddy") is not { } plumbBuddySupportDiscord)
                            return;
                        await dialogService.ShowSupportDiscordStepsDialogAsync("PlumbBuddy", plumbBuddySupportDiscord);
                    };
                });
        }
        return false;
    }

    void ConfigureLifetimeScope(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<ModSettingScan>().As<IModSettingScan>();
        containerBuilder.RegisterType<ScriptModSettingScan>().As<IScriptModSettingScan>();
        containerBuilder.RegisterType<ShowModListStartupSettingScan>().As<IShowModListStartupSettingScan>();
        containerBuilder.RegisterType<PackageDepthScan>().As<IPackageDepthScan>();
        containerBuilder.RegisterType<Ts4ScriptDepthScan>().As<ITs4ScriptDepthScan>();
        containerBuilder.RegisterType<LooseZipArchiveScan>().As<ILooseZipArchiveScan>();
        containerBuilder.RegisterType<LooseRarArchiveScan>().As<ILooseRarArchiveScan>();
        containerBuilder.RegisterType<Loose7ZipArchiveScan>().As<ILoose7ZipArchiveScan>();
        containerBuilder.RegisterType<PackageCorruptScan>().As<IPackageCorruptScan>();
        containerBuilder.RegisterType<Ts4ScriptCorruptScan>().As<ITs4ScriptCorruptScan>();
        containerBuilder.RegisterType<ErrorLogScan>().As<IErrorLogScan>();
        containerBuilder.RegisterType<McccMissingScan>().As<IMcccMissingScan>();
        containerBuilder.RegisterType<BeMissingScan>().As<IBeMissingScan>();
        containerBuilder.RegisterType<ModGuardMissingScan>().As<IModGuardMissingScan>();
        containerBuilder.RegisterType<DependencyScan>().As<IDependencyScan>();
        containerBuilder.RegisterType<ExclusivityScan>().As<IExclusivityScan>();
        containerBuilder.RegisterType<CacheStalenessScan>().As<ICacheStalenessScan>();
        containerBuilder.RegisterType<MultipleModVersionsScan>().As<IMultipleModVersionsScan>();
        containerBuilder.RegisterType<MismatchedInscribedHashesScan>().As<IMismatchedInscribedHashesScan>();
    }

    void ConnectToInstallationDirectory()
    {
        IDisposable? fileSystemWatcherConnectionLockHeld = null;
        try
        {
            try
            {
                fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
                if (fileSystemWatcherConnectionLockHeld is null)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (settings.Onboarded && Directory.Exists(settings.InstallationFolderPath))
            {
                if (installationDirectoryWatcher is null)
                {
                    ResampleGameVersion();
                    try
                    {
                        installationDirectoryWatcher = new FileSystemWatcher(settings.InstallationFolderPath)
                        {
                            IncludeSubdirectories = true,
                            InternalBufferSize = 64 * 1024,
                            NotifyFilter =
                                  NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Size
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "unhandled exception when initializing game installation monitoring at {Path}", settings.InstallationFolderPath);
                        return;
                    }
                    installationDirectoryWatcher.Changed += InstallationDirectoryFileSystemEntryChangedHandler;
                    installationDirectoryWatcher.Created += InstallationDirectoryFileSystemEntryCreatedHandler;
                    installationDirectoryWatcher.Deleted += InstallationDirectoryFileSystemEntryDeletedHandler;
                    installationDirectoryWatcher.Error += InstallationDirectoryWatcherErrorHandler;
                    installationDirectoryWatcher.Renamed += InstallationDirectoryFileSystemEntryRenamedHandler;
                    installationDirectoryWatcher.EnableRaisingEvents = true;
                }
                CheckIfSteam();
                if (packsDirectoryWatcher is null)
                {
                    packsDirectoryWatcher = new FileSystemWatcher(PacksDirectoryPath)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter =
                              NotifyFilters.CreationTime
                            | NotifyFilters.DirectoryName
                            | NotifyFilters.FileName
                            | NotifyFilters.LastWrite
                            | NotifyFilters.Size
                    };
                    packsDirectoryWatcher.Changed += PacksDirectoryFileSystemEntryChangedHandler;
                    packsDirectoryWatcher.Created += PacksDirectoryFileSystemEntryCreatedHandler;
                    packsDirectoryWatcher.Deleted += PacksDirectoryFileSystemEntryDeletedHandler;
                    packsDirectoryWatcher.Error += PacksDirectoryWatcherErrorHandler;
                    packsDirectoryWatcher.Renamed += PacksDirectoryFileSystemEntryRenamedHandler;
                    packsDirectoryWatcher.EnableRaisingEvents = true;
                }
                ResampleInstalledPackCodes();
                UpdateScanInitializationStatus();
            }
        }
        finally
        {
            fileSystemWatcherConnectionLockHeld?.Dispose();
        }
    }

    async void ConnectToUserDataDirectory()
    {
        IDisposable? fileSystemWatcherConnectionLockHeld = null;
        try
        {
            try
            {
                fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
                if (fileSystemWatcherConnectionLockHeld is null)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (settings.Onboarded && Directory.Exists(settings.UserDataFolderPath))
            {
                ResampleGameOptions();
                cacheComponents =
                [
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "avatarcache.package")),
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "clientDB.package")),
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "houseDescription-client.package")),
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "localsimtexturecache.package")),
                    new FileInfo(Path.Combine(settings.UserDataFolderPath, "localthumbcache.package")),
                    new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "cachestr")),
                    new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "onlinethumbnailcache"))
                ];
                ResampleCacheClarity();
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                var userDataFolderTextAndHtmlFiles = new DirectoryInfo(settings.UserDataFolderPath)
                    .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => (f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                        || f.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
                        || f.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                        || f.Extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                        && (f.Name.Contains("exception", StringComparison.OrdinalIgnoreCase)
                        || f.Name.Contains("crash", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                var userDataFolderTextAndHtmlFilesOfInterest = await pbDbContext.FilesOfInterest
                    .Where(foi => foi.Path.Replace($"{Path.DirectorySeparatorChar}", string.Empty) == foi.Path
                        && (foi.Path.ToUpper().Replace("EXCEPTION", string.Empty) != foi.Path.ToUpper()
                        || foi.Path.ToUpper().Replace("CRASH", string.Empty) != foi.Path.ToUpper())
                        && (foi.FileType == ModsDirectoryFileType.TextFile || foi.FileType == ModsDirectoryFileType.HtmlFile))
                    .ToListAsync().ConfigureAwait(false);
                await pbDbContext.FilesOfInterest.AddRangeAsync(userDataFolderTextAndHtmlFiles
                    .Where(f => !userDataFolderTextAndHtmlFilesOfInterest.Any(foi => foi.Path == f.Name))
                    .Select(f =>
                        new FileOfInterest
                        {
                            Path = f.Name,
                            FileType = f.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
                            ? ModsDirectoryFileType.HtmlFile
                            : ModsDirectoryFileType.TextFile
                        })).ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                var existingFileOfInterestPaths = userDataFolderTextAndHtmlFiles
                    .Select(f => f.Name)
                    .ToList();
                await pbDbContext.FilesOfInterest
                    .Where(foi => foi.Path.Replace($"{Path.DirectorySeparatorChar}", string.Empty).Length == foi.Path.Length
                        && !Enumerable.Contains(existingFileOfInterestPaths, foi.Path))
                    .ExecuteDeleteAsync().ConfigureAwait(false);
                if (userDataDirectoryWatcher is not null)
                    return;
                userDataDirectoryWatcher = new FileSystemWatcher(settings.UserDataFolderPath)
                {
                    IncludeSubdirectories = true,
                    InternalBufferSize = 64 * 1024,
                    NotifyFilter =
                          NotifyFilters.CreationTime
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.FileName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Size
                };
                userDataDirectoryWatcher.Changed += UserDataDirectoryFileSystemEntryChangedHandler;
                userDataDirectoryWatcher.Created += UserDataDirectoryFileSystemEntryCreatedHandler;
                userDataDirectoryWatcher.Deleted += UserDataDirectoryFileSystemEntryDeletedHandler;
                userDataDirectoryWatcher.Error += UserDataDirectoryWatcherErrorHandler;
                userDataDirectoryWatcher.Renamed += UserDataDirectoryFileSystemEntryRenamedHandler;
                userDataDirectoryWatcher.EnableRaisingEvents = true;
                UpdateScanInitializationStatus();
                NoticeIfGameIsRunning();
                modsDirectoryCataloger.Catalog(string.Empty);
                FreshenIntegration(force: true);
            }
        }
        finally
        {
            fileSystemWatcherConnectionLockHeld?.Dispose();
        }
    }

    void DisconnectFromInstallationDirectoryWatcher()
    {
        IDisposable? fileSystemWatcherConnectionLockHeld = null;
        try
        {
            try
            {
                fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
                if (fileSystemWatcherConnectionLockHeld is null)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (installationDirectoryWatcher is not null)
            {
                installationDirectoryWatcher.Changed -= InstallationDirectoryFileSystemEntryChangedHandler;
                installationDirectoryWatcher.Created -= InstallationDirectoryFileSystemEntryCreatedHandler;
                installationDirectoryWatcher.Deleted -= InstallationDirectoryFileSystemEntryDeletedHandler;
                installationDirectoryWatcher.Error -= InstallationDirectoryWatcherErrorHandler;
                installationDirectoryWatcher.Renamed -= InstallationDirectoryFileSystemEntryRenamedHandler;
                installationDirectoryWatcher.Dispose();
                installationDirectoryWatcher = null;
            }
            if (packsDirectoryWatcher is not null)
            {
                packsDirectoryWatcher.Changed -= PacksDirectoryFileSystemEntryChangedHandler;
                packsDirectoryWatcher.Created -= PacksDirectoryFileSystemEntryCreatedHandler;
                packsDirectoryWatcher.Deleted -= PacksDirectoryFileSystemEntryDeletedHandler;
                packsDirectoryWatcher.Error -= PacksDirectoryWatcherErrorHandler;
                packsDirectoryWatcher.Renamed -= PacksDirectoryFileSystemEntryRenamedHandler;
                packsDirectoryWatcher.Dispose();
                packsDirectoryWatcher = null;
            }
        }
        finally
        {
            fileSystemWatcherConnectionLockHeld?.Dispose();
        }
    }

    void DisconnectFromUserDataDirectoryWatcher()
    {
        IDisposable? fileSystemWatcherConnectionLockHeld = null;
        try
        {
            try
            {
                fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
                if (fileSystemWatcherConnectionLockHeld is null)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (userDataDirectoryWatcher is not null)
            {
                var integrationPackageFile = new FileInfo(Path.Combine(userDataDirectoryWatcher.Path, "Mods", IntegrationPackageName));
                var integrationScriptModFile = new FileInfo(Path.Combine(userDataDirectoryWatcher.Path, "Mods", IntegrationScriptModName));
                userDataDirectoryWatcher.Changed -= UserDataDirectoryFileSystemEntryChangedHandler;
                userDataDirectoryWatcher.Created -= UserDataDirectoryFileSystemEntryCreatedHandler;
                userDataDirectoryWatcher.Deleted -= UserDataDirectoryFileSystemEntryDeletedHandler;
                userDataDirectoryWatcher.Error -= UserDataDirectoryWatcherErrorHandler;
                userDataDirectoryWatcher.Renamed -= UserDataDirectoryFileSystemEntryRenamedHandler;
                userDataDirectoryWatcher.Dispose();
                userDataDirectoryWatcher = null;
                if (integrationPackageFile.Exists)
                {
                    try
                    {
                        integrationPackageFile.Delete();
                    }
                    catch (IOException)
                    {
                    }
                    integrationPackageLastSha256 = ImmutableArray<byte>.Empty;
                }
                if (integrationScriptModFile.Exists)
                {
                    try
                    {
                        integrationScriptModFile.Delete();
                    }
                    catch (IOException)
                    {
                    }
                    integrationScriptModLastSha256 = ImmutableArray<byte>.Empty;
                }
                cacheComponents = [];
                IsModsDisabledGameSettingOn = false;
                IsScriptModsEnabledGameSettingOn = true;
                IsShowModListStartupGameSettingOn = false;
            }
        }
        finally
        {
            fileSystemWatcherConnectionLockHeld?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (launcherUserDataDirectoryWatcher is { } activeLauncherUserDataDirectoryWatcher)
            {
                launcherUserDataDirectoryWatcher = null;
                activeLauncherUserDataDirectoryWatcher.Changed -= HandleLauncherUserDataDirectoryWatcherEvent;
                activeLauncherUserDataDirectoryWatcher.Created -= HandleLauncherUserDataDirectoryWatcherEvent;
                activeLauncherUserDataDirectoryWatcher.Deleted -= HandleLauncherUserDataDirectoryWatcherEvent;
                activeLauncherUserDataDirectoryWatcher.Renamed -= HandleLauncherUserDataDirectoryWatcherEvent;
                activeLauncherUserDataDirectoryWatcher.Error -= HandleLauncherUserDataDirectoryWatcherError;
                activeLauncherUserDataDirectoryWatcher.Dispose();
            }
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            lifetimeScope.Dispose();
        }
    }

    void FreshenIntegration(bool force = false)
    {
        if (!settings.GenerateGlobalManifestPackage)
            return;
        _ = Task.Run(() => FreshenIntegrationAsync(force));
    }

    async Task FreshenIntegrationAsync(bool force = false)
    {
        var enqueuedFresheningTaskLockPotentiallyHeld = await enqueuedFresheningTaskLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
        if (enqueuedFresheningTaskLockPotentiallyHeld is null)
            return;
        using var fresheningTaskLockHeld = await fresheningTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedFresheningTaskLockPotentiallyHeld.Dispose();
        var modsDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "Mods"));
        if (!modsDirectory.Exists)
            return;
        await Task.Delay(500).ConfigureAwait(false);
        var integrationPackageFileInfo = new FileInfo(Path.Combine(modsDirectory.FullName, IntegrationPackageName));
        if (force
            || !integrationPackageFileInfo.Exists
            || !integrationPackageLastSha256.SequenceEqual(await ModFileManifestModel.GetFileSha256HashAsync(integrationPackageFileInfo.FullName).ConfigureAwait(false)))
        {
            var manifestedModFiles = new List<GlobalModsManifestModelManifestedModFile>();
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            foreach (var modFileHashElements in await pbDbContext.ModFileHashes
                .Where(mfh => mfh.ModFiles.Any() && mfh.ModFileManifests.Any())
                .Select(mfh => new
                {
                    Paths = mfh.ModFiles.Select(mf => mf.Path!).ToList(),
                    Manifests = mfh.ModFileManifests.Select(mfm => new
                    {
                        mfm.Key,
                        mfm.TuningName,
                        CalculatedSha256 = mfm.CalculatedModFileManifestHash.Sha256,
                        SubsumedSha256 = mfm.SubsumedHashes.Select(mfmh => mfmh.Sha256).ToList()
                    }).ToList()
                })
                .ToListAsync()
                .ConfigureAwait(false))
                foreach (var manifest in modFileHashElements.Manifests)
                {
                    var hashes = manifest.SubsumedSha256
                        .Append(manifest.CalculatedSha256)
                        .Select(byteArray => byteArray.ToImmutableArray())
                        .Select(ia => ia.ToHexString())
                        .Distinct()
                        .Select(hex => hex.ToByteSequence().ToImmutableArray())
                        .ToImmutableArray();
                    manifestedModFiles.AddRange(modFileHashElements.Paths.Select(path =>
                    {
                        var manifestedModFile = new GlobalModsManifestModelManifestedModFile
                        {
                            ModsFolderPath = path,
                            ManifestKey = manifest.Key,
                            ManifestTuningName = manifest.TuningName
                        };
                        manifestedModFile.Hashes.UnionWith(hashes);
                        return manifestedModFile;
                    }));
                }
            var model = new GlobalModsManifestModel();
            foreach (var packCode in InstalledPackCodes)
                model.InstalledPacks.Add(packCode);
            foreach (var packCode in DisabledPackCodes)
                model.DisabledPacks.Add(packCode);
            foreach (var manifestedModFile in manifestedModFiles.OrderBy(mfm => mfm.ModsFolderPath))
                model.ManifestedModFiles.Add(manifestedModFile);
            using var integrationPackage = new DataBasePackedFile();
            await integrationPackage.SetAsync(GlobalModsManifestModel.ResourceKey, model).ConfigureAwait(false);
            await integrationPackage.SaveAsAsync(integrationPackageFileInfo.FullName).ConfigureAwait(false);
            integrationPackageLastSha256 = await ModFileManifestModel.GetFileSha256HashAsync(integrationPackageFileInfo.FullName).ConfigureAwait(false);
        }
        var integrationScriptModFileInfo = new FileInfo(Path.Combine(modsDirectory.FullName, IntegrationScriptModName));
        if (force
            || !integrationScriptModFileInfo.Exists
            || !integrationScriptModLastSha256.SequenceEqual(await ModFileManifestModel.GetFileSha256HashAsync(integrationScriptModFileInfo.FullName).ConfigureAwait(false)))
        {
            using var integrationScriptModResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlumbBuddy.PlumbBuddy_Proxy.ts4script");
            if (integrationScriptModResourceStream is not null)
            {
                using (var integrationScriptModFileStream = integrationScriptModFileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.None))
                    await integrationScriptModResourceStream.CopyToAsync(integrationScriptModFileStream).ConfigureAwait(false);
                integrationScriptModLastSha256 = await ModFileManifestModel.GetFileSha256HashAsync(integrationScriptModFileInfo.FullName).ConfigureAwait(false);
            }
        }
    }

    string GetRelativePathInUserDataFolder(string fullPath)
    {
        var trimmedLocalPathSegmentsMatch = trimmedLocalPathSegmentsPattern.Match(settings.UserDataFolderPath);
        if (!trimmedLocalPathSegmentsMatch.Success)
            throw new InvalidOperationException("User data folder path is not a valid path");
        try
        {
            return fullPath[(trimmedLocalPathSegmentsMatch.Groups["path"].Value.Length + 1)..];
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Path is not valid or not within the user data folder", nameof(fullPath), ex);
        }
    }

    void HandleLauncherUserDataDirectoryWatcherError(object sender, ErrorEventArgs e) =>
        ResetLauncherUserDataMonitoring();

    void HandleLauncherUserDataDirectoryWatcherEvent(object sender, FileSystemEventArgs e) =>
        ResampleDisabledPacks();

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IModsDirectoryCataloger.State))
        {
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Cataloging)
                FreshenIntegration(force: true);
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.AnalyzingTopology or ModsDirectoryCatalogerState.Idle)
                Scan();
        }
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Type))
        {
            Scan();
            ApplyGameProcessEnhancements();
        }
        else if (e.PropertyName is nameof(ISettings.CacheStatus))
            Scan();
        else if (e.PropertyName is nameof(ISettings.ForceGameProcessPerformanceProcessorAffinity))
            ApplyGameProcessEnhancements();
        else if (e.PropertyName is nameof(ISettings.GenerateGlobalManifestPackage))
        {
            var integrationPackageFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", IntegrationPackageName));
            var integrationScriptModFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", IntegrationScriptModName));
            if (!settings.GenerateGlobalManifestPackage && (integrationPackageFile.Exists || integrationScriptModFile.Exists))
            {
                if (integrationPackageFile.Exists)
                    integrationPackageFile.Delete();
                if (integrationScriptModFile.Exists)
                    integrationScriptModFile.Delete();
            }
            else if (settings.GenerateGlobalManifestPackage)
                FreshenIntegration(force: true);
        }
        else if (e.PropertyName is nameof(ISettings.InstallationFolderPath))
        {
            DisconnectFromInstallationDirectoryWatcher();
            ConnectToInstallationDirectory();
        }
        else if (e.PropertyName is nameof(ISettings.Onboarded))
        {
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToInstallationDirectory();
            ConnectToUserDataDirectory();
        }
        else if (e.PropertyName is nameof(ISettings.UserDataFolderPath))
        {
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToUserDataDirectory();
        }
        else if ((e.PropertyName?.StartsWith("Scan", StringComparison.OrdinalIgnoreCase) ?? false) && settings.Onboarded)
            UpdateScanInitializationStatus();
    }

    public async Task HelpWithPackPurchaseAsync(string packCode, IDialogService dialogService, IReadOnlyList<string>? creators, string? electronicArtsPromoCode)
    {
        if (!IsSteamInstallation)
        {
            if (creators?.Count is > 0
                && electronicArtsPromoCode is not null)
            {
                if (await dialogService.ShowQuestionDialogAsync(AppText.SmartSimObserver_HelpWithPackPurchase_PresentOpportunity_Caption, string.Format(AppText.SmartSimObserver_HelpWithPackPurchase_PresentOpportunity_Text, creators.Humanize(), electronicArtsPromoCode, packCode), true) is not { } copyToClipboardPreference)
                    return;
                if (copyToClipboardPreference)
                {
                    await Clipboard.SetTextAsync(electronicArtsPromoCode);
                    await dialogService.ShowSuccessDialogAsync(AppText.SmartSimObserver_HelpWithPackPurchase_Thanks_Caption, string.Format(AppText.SmartSimObserver_HelpWithPackPurchase_Thanks_Text, electronicArtsPromoCode, creators.Humanize()));
                }
            }
            else if ((publicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDesription) ?? false)
                && !string.IsNullOrWhiteSpace(packDesription.EaPromoCode))
            {
                if (await dialogService.ShowQuestionDialogAsync(AppText.SmartSimObserver_HelpWithPackPurchase_PresentOpportunity_Caption, string.Format(AppText.SmartSimObserver_HelpWithPackPurchase_PresentCreatorKitOpportunity_Text, packDesription.EaPromoCode, packCode), true) is not { } copyToClipboardPreference)
                    return;
                if (copyToClipboardPreference)
                {
                    await Clipboard.SetTextAsync(packDesription.EaPromoCode);
                    await dialogService.ShowSuccessDialogAsync(AppText.SmartSimObserver_HelpWithPackPurchase_Thanks_Caption, string.Format(AppText.SmartSimObserver_HelpWithCreatorKitPackPurchase_Thanks_Text, packDesription.EaPromoCode));
                }
            }
        }
        await Browser.OpenAsync($"https://plumbbuddy.app/redirect?purchase-pack={packCode}{(IsSteamInstallation ? "&from=steam" : string.Empty)}", BrowserLaunchMode.External);
    }

    void InstallationDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e) =>
        resampleGameVersionDebouncer.Execute();

    void InstallationDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e) =>
        resampleGameVersionDebouncer.Execute();

    void InstallationDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e) =>
        resampleGameVersionDebouncer.Execute();

    void InstallationDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e) =>
        resampleGameVersionDebouncer.Execute();

    void InstallationDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromInstallationDirectoryWatcher();
        ConnectToInstallationDirectory();
    }

    bool IsCacheLocked()
    {
        try
        {
            foreach (var fileInfo in cacheComponents.OfType<FileInfo>())
            {
                fileInfo.Refresh();
                if (fileInfo.Exists)
                {
                    using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
            }
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    void NoticeIfGameIsRunning()
    {
        if (modsDirectoryCataloger.State is not ModsDirectoryCatalogerState.Sleeping
            && (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            || IsCacheLocked())
            Task.Run(NoticeIfGameIsRunningAsync);
    }

    async Task NoticeIfGameIsRunningAsync()
    {
        if (await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false) is { } ts4Process)
        {
            modsDirectoryCataloger.GoToSleep();
            await ApplyGameProcessEnhancementsAsync(ts4Process).ConfigureAwait(false);
            await ts4Process.WaitForExitAsync().ConfigureAwait(false);
            IsPerformanceProcessorAffinityInEffect = false;
            ts4Process.Dispose();
            modsDirectoryCataloger.WakeUp();
        }
    }

    bool NotifyProxyHostOfScreenshotChangesIfTheyChanged(string relativePath)
    {
        if (relativePath.StartsWith($"Screenshots{Path.DirectorySeparatorChar}"))
        {
            _ = Task.Run(async () => await proxyHost.NotifyScreenshotsChangedAsync().ConfigureAwait(false));
            return true;
        }
        return false;
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public async Task OpenDownloadsFolderAsync()
    {
        var downloadsFolder = new DirectoryInfo(settings.DownloadsFolderPath);
        if (downloadsFolder.Exists)
        {
            platformFunctions.ViewDirectory(downloadsFolder);
            return;
        }
        await blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowErrorDialogAsync(AppText.SmartSimObserver_OpenDownloadsFolder_Error_Caption, string.Format(AppText.SmartSimObserver_OpenDownloadsFolder_Error_Text, settings.DownloadsFolderPath)).ConfigureAwait(false);
    }

    public void OpenModsFolder()
    {
        if (settings.Onboarded)
        {
            var modsDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "Mods"));
            if (modsDirectory.Exists)
                platformFunctions.ViewDirectory(modsDirectory);
        }
    }

    void PacksDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e) =>
        ResampleInstalledPackCodes();

    void PacksDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromInstallationDirectoryWatcher();
        ConnectToInstallationDirectory();
    }

    void ResampleCacheClarity()
    {
        foreach (var cacheComponent in cacheComponents)
            cacheComponent.Refresh();
        var anyCacheComponentsExistOnDisk = cacheComponents.Any(ce => ce is DirectoryInfo dce && dce.Exists ? dce.GetFiles("*.*", SearchOption.AllDirectories).Length > 0 : ce.Exists);
        if (settings.CacheStatus is SmartSimCacheStatus.Clear && anyCacheComponentsExistOnDisk)
        {
            settings.CacheStatus = SmartSimCacheStatus.Normal;
            NoticeIfGameIsRunning();
        }
        else if (settings.CacheStatus is not SmartSimCacheStatus.Clear && !anyCacheComponentsExistOnDisk)
            settings.CacheStatus = SmartSimCacheStatus.Clear;
    }

    void ResampleDisabledPacks() =>
        _ = Task.Run(ResampleDisabledPacksAsync);

    async Task ResampleDisabledPacksAsync()
    {
        var currentlyDisabledPacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var commandLineArgumentsLine = IsSteamInstallation
            ? await steam.GetTS4ConfiguredCommandLineArgumentsAsync().ConfigureAwait(false)
            : await electronicArtsApp.GetTS4ConfiguredCommandLineArgumentsAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(commandLineArgumentsLine)
            && GetDisablePacksCommandLineArgumentPattern().Match(commandLineArgumentsLine) is { Success: true } match)
            foreach (var packCode in match.Groups["packCodes"].Value.Split(","))
                currentlyDisabledPacks.Add(packCode);
        DisabledPackCodes = currentlyDisabledPacks.ToList();
    }

    void ResampleGameOptions() =>
        _ = Task.Run(ResampleGameOptionsAsync);

    async Task ResampleGameOptionsAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        var previousIsModsDisabledGameSettingOn = IsModsDisabledGameSettingOn;
        var previousIsScriptModsEnabledGameSettingOn = IsScriptModsEnabledGameSettingOn;
        var previousIsShowModListStartupGameSettingOn = IsShowModListStartupGameSettingOn;
        var parsedSuccessfully = false;
        var optionsIniFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Options.ini"));
        if (optionsIniFile.Exists)
        {
            try
            {
                var parser = new IniDataParser();
                var data = parser.Parse(await File.ReadAllTextAsync(optionsIniFile.FullName).ConfigureAwait(false));
                var optionsData = data["options"];
                IsModsDisabledGameSettingOn = optionsData["modsdisabled"] == "1";
                IsScriptModsEnabledGameSettingOn = optionsData["scriptmodsenabled"] == "1";
                IsShowModListStartupGameSettingOn = optionsData["showmodliststartup"] == "1";
                parsedSuccessfully = true;
            }
            catch (ParsingException ex)
            {
                // eww, a bad INI file?
                logger.LogWarning(ex, "attempting to parse the game options INI file at {path} failed", optionsIniFile.FullName);
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.SmartSimObserver_Error_CannotReadGameSettings, optionsIniFile.FullName, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.CogOff;
                });
            }
        }
        if (!parsedSuccessfully)
        {
            IsModsDisabledGameSettingOn = true;
            IsScriptModsEnabledGameSettingOn = false;
            IsShowModListStartupGameSettingOn = true;
        }
        if (IsModsDisabledGameSettingOn != previousIsModsDisabledGameSettingOn
            || IsScriptModsEnabledGameSettingOn != previousIsScriptModsEnabledGameSettingOn
            || IsShowModListStartupGameSettingOn != previousIsShowModListStartupGameSettingOn)
            Scan();
    }

    void ResampleGameVersion() =>
        _ = Task.Run(ResampleGameVersionAsync);

    async Task ResampleGameVersionAsync() =>
        GameVersion = await platformFunctions.GetTS4InstallationVersionAsync().ConfigureAwait(false);

    bool ResampleGameOptionsIfTheyChanged(string relativePath)
    {
        if (relativePath.Equals("Options.ini", fileSystemStringComparison))
        {
            Task.Run(ResampleGameOptionsAsync);
            return true;
        }
        return false;
    }

    void ResampleInstalledPackCodes() =>
        _ = Task.Run(ResampleInstalledPackCodesAsync);

    async Task ResampleInstalledPackCodesAsync()
    {
        gameResourceCataloger.ScanSoon();
        var enqueuedResamplingPacksTaskLockPotentiallyHeld = await enqueuedResamplingPacksTaskLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
        if (enqueuedResamplingPacksTaskLockPotentiallyHeld is null)
            return;
        using var resamplingPacksTaskLockHeld = await resamplingPacksTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedResamplingPacksTaskLockPotentiallyHeld.Dispose();
        if (installedPackCodes.Count is > 0)
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var packsDirectory = new DirectoryInfo(PacksDirectoryPath);
        if (!packsDirectory.Exists)
            return;
        InstalledPackCodes = [..packsDirectory.GetDirectories().Select(directoryInfo => directoryInfo.Name)];
    }

    void ResetLauncherUserDataMonitoring() =>
        _ = Task.Run(ResetLauncherUserDataMonitoringAsync);

    async Task ResetLauncherUserDataMonitoringAsync()
    {
        if (launcherUserDataDirectoryWatcher is { } activeWatcher)
        {
            launcherUserDataDirectoryWatcher = null;
            activeWatcher.Changed -= HandleLauncherUserDataDirectoryWatcherEvent;
            activeWatcher.Created -= HandleLauncherUserDataDirectoryWatcherEvent;
            activeWatcher.Deleted -= HandleLauncherUserDataDirectoryWatcherEvent;
            activeWatcher.Renamed -= HandleLauncherUserDataDirectoryWatcherEvent;
            activeWatcher.Error -= HandleLauncherUserDataDirectoryWatcherError;
            activeWatcher.Dispose();
        }
        if (await (IsSteamInstallation ? steam.GetSteamUserDataDirectoryAsync() : electronicArtsApp.GetElectronicArtsUserDataDirectoryAsync()).ConfigureAwait(false) is not { } launcherUserDataDirectory)
            return;
        var newWatcher = new FileSystemWatcher(launcherUserDataDirectory.FullName)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024,
            NotifyFilter =
                  NotifyFilters.CreationTime
                | NotifyFilters.DirectoryName
                | NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
        };
        newWatcher.Changed += HandleLauncherUserDataDirectoryWatcherEvent;
        newWatcher.Created += HandleLauncherUserDataDirectoryWatcherEvent;
        newWatcher.Deleted += HandleLauncherUserDataDirectoryWatcherEvent;
        newWatcher.Renamed += HandleLauncherUserDataDirectoryWatcherEvent;
        newWatcher.Error += HandleLauncherUserDataDirectoryWatcherError;
        newWatcher.EnableRaisingEvents = true;
        launcherUserDataDirectoryWatcher = newWatcher;
    }

    public void Scan() =>
        Task.Run(ScanAsync);

    async Task ScanAsync()
    {
        var enqueuedScanningTaskLockPotentiallyHeld = await enqueuedScanningTaskLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
        if (enqueuedScanningTaskLockPotentiallyHeld is null)
            return;
        try
        {
            using var scanningTaskLockHeld = await scanningTaskLock.LockAsync().ConfigureAwait(false);
            enqueuedScanningTaskLockPotentiallyHeld.Dispose();
            if (modsDirectoryCataloger.State is not (ModsDirectoryCatalogerState.AnalyzingTopology or ModsDirectoryCatalogerState.Idle))
                return;
            IsScanning = true;
            var scanIssues = new ConcurrentBag<ScanIssue>();
            using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4)))
            {
                await Task.WhenAll(scanInstances.Values.Select(async scanInstance =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await foreach (var scanIssue in scanInstance.ScanAsync())
                            scanIssues.Add(scanIssue);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })).ConfigureAwait(false);
            }
            ScanIssues = [..scanIssues.OrderByDescending(scanIssue => scanIssue.Type).ThenBy(scanIssue => scanIssue.Caption)];
            var attentionWorthyScanIssues = scanIssues.Where(si => si.Type is not ScanIssueType.Healthy).ToImmutableArray();
            if (attentionWorthyScanIssues.Length is > 0)
            {
                var distinctAttentionWorthyScanIssueCaptions = attentionWorthyScanIssues.Select(si => si.Caption).Distinct().ToImmutableArray();
                string modHealthStatusSummary;
                if (distinctAttentionWorthyScanIssueCaptions.Length is <= 3)
                    modHealthStatusSummary = distinctAttentionWorthyScanIssueCaptions.Humanize();
                else
                    modHealthStatusSummary = distinctAttentionWorthyScanIssueCaptions.Take(2).Append(AppText.SmartSimObserver_Notification_UnhealthyMods_Text_OtherIssue.ToQuantity(distinctAttentionWorthyScanIssueCaptions.Length - 2, ShowQuantityAs.Words)).Humanize();
                if (modHealthStatusSummary != lastModHealthStatusSummary)
                {
                    if (!appLifecycleManager.IsVisible)
                        await platformFunctions.SendLocalNotificationAsync(AppText.SmartSimObserver_Notification_UnhealthyMods_Caption, string.Format(AppText.SmartSimObserver_Notification_UnhealthyMods_Text, modHealthStatusSummary)).ConfigureAwait(false);
                    lastModHealthStatusSummary = modHealthStatusSummary;
                }
            }
            else
                lastModHealthStatusSummary = string.Empty;
            await platformFunctions.SetBadgeNumberAsync(attentionWorthyScanIssues.Length).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unexpected exception encountered while scanning");
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.SmartSimObserver_Error_ModsHealthScanFailed, ex.GetType().Name, ex.Message)), Severity.Error, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.BottleTonicPlus;
                options.RequireInteraction = true;
            });
            await platformFunctions.SetBadgeNumberAsync(0).ConfigureAwait(false);
        }
        finally
        {
            IsScanning = false;
        }
    }

    void UpdateScanInitializationStatus() =>
        Task.Run(UpdateScanInitializationStatusWork);

    void UpdateScanInitializationStatusWork()
    {
        try
        {
            var fullyConnectedToGame = installationDirectoryWatcher is not null && userDataDirectoryWatcher is not null;
            var scansInitialized = scanInstances.Count > 0;
            if (!fullyConnectedToGame && scansInitialized)
            {
                while (scanInstances.Keys.FirstOrDefault() is { } key)
                    if (scanInstances.TryRemove(key, out var scan))
                        scan.Dispose();
                ScanIssues = [];
                return;
            }
            if (fullyConnectedToGame)
            {
                var initializationChange = false;
                bool checkScanInitialization(bool playerHasScanEnabled, Type scanInterface)
                {
                    if (playerHasScanEnabled && !scanInstances.ContainsKey(scanInterface))
                    {
                        scanInstances.AddOrUpdate(scanInterface, AddScanInstancesValueFactory, UpdateScanInstancesValueFactory);
                        return true;
                    }
                    if (!playerHasScanEnabled && scanInstances.TryRemove(scanInterface, out var scanInstance))
                    {
                        scanInstance.Dispose();
                        return true;
                    }
                    return false;
                }
                initializationChange |= checkScanInitialization(settings.ScanForModsDisabled, typeof(IModSettingScan));
                initializationChange |= checkScanInitialization(settings.ScanForScriptModsDisabled, typeof(IScriptModSettingScan));
                initializationChange |= checkScanInitialization(settings.ScanForShowModsListAtStartupEnabled, typeof(IShowModListStartupSettingScan));
                initializationChange |= checkScanInitialization(settings.ScanForInvalidModSubdirectoryDepth, typeof(IPackageDepthScan));
                initializationChange |= checkScanInitialization(settings.ScanForInvalidScriptModSubdirectoryDepth, typeof(ITs4ScriptDepthScan));
                initializationChange |= checkScanInitialization(settings.ScanForLooseZipArchives, typeof(ILooseZipArchiveScan));
                initializationChange |= checkScanInitialization(settings.ScanForLooseRarArchives, typeof(ILooseRarArchiveScan));
                initializationChange |= checkScanInitialization(settings.ScanForLoose7ZipArchives, typeof(ILoose7ZipArchiveScan));
                initializationChange |= checkScanInitialization(settings.ScanForCorruptMods, typeof(IPackageCorruptScan));
                initializationChange |= checkScanInitialization(settings.ScanForCorruptScriptMods, typeof(ITs4ScriptCorruptScan));
                initializationChange |= checkScanInitialization(settings.ScanForErrorLogs, typeof(IErrorLogScan));
                initializationChange |= checkScanInitialization(settings.ScanForMissingMccc, typeof(IMcccMissingScan));
                initializationChange |= checkScanInitialization(settings.ScanForMissingBe, typeof(IBeMissingScan));
                initializationChange |= checkScanInitialization(settings.ScanForMissingModGuard, typeof(IModGuardMissingScan));
                initializationChange |= checkScanInitialization(settings.ScanForMissingDependency, typeof(IDependencyScan));
                initializationChange |= checkScanInitialization(settings.ScanForMutuallyExclusiveMods, typeof(IExclusivityScan));
                initializationChange |= checkScanInitialization(settings.ScanForCacheStaleness, typeof(ICacheStalenessScan));
                initializationChange |= checkScanInitialization(settings.ScanForMultipleModVersions, typeof(IMultipleModVersionsScan));
                initializationChange |= checkScanInitialization(settings.ScanForMismatchedInscribedHashes, typeof(IMismatchedInscribedHashesScan));
                if (initializationChange)
                    Scan();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unexpected exception encountered while initializing scans");
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.SmartSimObserver_Error_InitializingScansFailed, ex.GetType().Name, ex.Message)), Severity.Error, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.BottleTonicPlus;
                options.RequireInteraction = true;
            });
            platformFunctions.SetBadgeNumberAsync(0).Wait();
        }
    }

    IScan AddScanInstancesValueFactory(Type key) =>
        (IScan)lifetimeScope.Resolve(key);

    IScan UpdateScanInstancesValueFactory(Type key, IScan currentValue) =>
        currentValue;

    void UserDataDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfInModsDirectory(relativePath, out var integrationWasOverwritten))
            return;
        if (integrationWasOverwritten)
            FreshenIntegration();
        if (NotifyProxyHostOfScreenshotChangesIfTheyChanged(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e)
    {
        if (settings.CacheStatus is SmartSimCacheStatus.Clear && cacheComponents.Any(cc => e.FullPath.StartsWith(cc.FullName, fileSystemStringComparison)))
        {
            settings.CacheStatus = SmartSimCacheStatus.Normal;
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        var errorOrTraceLogInRootType = CatalogIfLikelyErrorOrTraceLogInRoot(relativePath);
        if (errorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored)
        {
            using var pbDbContext = pbDbContextFactory.CreateDbContext();
            pbDbContext.FilesOfInterest.Add(new()
            {
                Path = relativePath,
                FileType = errorOrTraceLogInRootType
            });
            try
            {
                pbDbContext.SaveChanges();
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                // do nothing, it's already there
            }
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
                Scan();
            return;
        }
        if (CatalogIfModsDirectory(relativePath))
        {
            FreshenIntegration();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var integrationWasOverwritten))
            return;
        if (integrationWasOverwritten)
            FreshenIntegration();
        if (NotifyProxyHostOfScreenshotChangesIfTheyChanged(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);
        if (settings.CacheStatus is not SmartSimCacheStatus.Clear && cacheComponents.Any(cc => Path.GetFullPath(cc.FullName).Equals(fullPath, fileSystemStringComparison)))
        {
            ResampleCacheClarity();
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        var errorOrTraceLogInRootType = CatalogIfLikelyErrorOrTraceLogInRoot(relativePath);
        if (errorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored)
        {
            using var pbDbContext = pbDbContextFactory.CreateDbContext();
            var relativePathToLower = relativePath.ToUpperInvariant();
            pbDbContext.FilesOfInterest.Where(foi => foi.Path.ToUpper() == relativePathToLower).ExecuteDelete();
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
                Scan();
            return;
        }
        if (CatalogIfModsDirectory(relativePath))
        {
            FreshenIntegration();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var integrationWasDeleted))
            return;
        if (integrationWasDeleted)
            FreshenIntegration();
        if (NotifyProxyHostOfScreenshotChangesIfTheyChanged(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = GetRelativePathInUserDataFolder(e.OldFullPath);
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(oldRelativePath) | ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        var oldErrorOrTraceLogInRootType = CatalogIfLikelyErrorOrTraceLogInRoot(oldRelativePath);
        if (oldErrorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored)
        {
            using var pbDbContext = pbDbContextFactory.CreateDbContext();
            var oldRelativePathToLower = relativePath.ToUpperInvariant();
            pbDbContext.FilesOfInterest.Where(foi => foi.Path.ToUpper() == oldRelativePathToLower).ExecuteDelete();
        }
        var errorOrTraceLogInRootType = CatalogIfLikelyErrorOrTraceLogInRoot(relativePath);
        if (errorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored)
        {
            using var pbDbContext = pbDbContextFactory.CreateDbContext();
            pbDbContext.FilesOfInterest.Add(new()
            {
                Path = relativePath,
                FileType = errorOrTraceLogInRootType
            });
            try
            {
                pbDbContext.SaveChanges();
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                // do nothing, it's already there
            }
        }
        if ((oldErrorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored || errorOrTraceLogInRootType is not ModsDirectoryFileType.Ignored)
            && modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            Scan();
        if (CatalogIfModsDirectory(oldRelativePath) | CatalogIfModsDirectory(relativePath))
        {
            FreshenIntegration();
            return;
        }
        if (CatalogIfInModsDirectory(oldRelativePath, out var integrationWasRenamed) | CatalogIfInModsDirectory(relativePath, out var integrationWasOverwritten))
            return;
        if (integrationWasRenamed || integrationWasOverwritten)
            FreshenIntegration();
        if (NotifyProxyHostOfScreenshotChangesIfTheyChanged(oldRelativePath) || NotifyProxyHostOfScreenshotChangesIfTheyChanged(relativePath))
            return;
    }

    void UserDataDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromUserDataDirectoryWatcher();
        ConnectToUserDataDirectory();
    }
}
