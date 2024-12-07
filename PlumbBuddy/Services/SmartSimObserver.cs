namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class SmartSimObserver :
    ISmartSimObserver
{
    static readonly Regex modsDirectoryRelativePathPattern = GetModsDirectoryRelativePathPattern();
    static readonly Regex trimmedLocalPathSegmentsPattern = GetTrimmedLocalPathSegmentsPattern();

    public const string GlobalModsManifestPackageName = "PlumbBuddy_GlobalModsManifest.package";

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

    [GeneratedRegex(@"^Mods[\\/].+$")]
    private static partial Regex GetModsDirectoryRelativePathPattern();

    [GeneratedRegex(@"^(?<path>.*?)[\\/]?$")]
    private static partial Regex GetTrimmedLocalPathSegmentsPattern();

    [GeneratedRegex(@"^\wp\d{2,}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetTs4PackCodePattern();

    public SmartSimObserver(ILifetimeScope lifetimeScope, ILogger<ISmartSimObserver> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, IAppLifecycleManager appLifecycleManager, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, IElectronicArtsApp electronicArtsApp, ISteam steam, ISuperSnacks superSnacks, IBlazorFramework blazorFramework, IPublicCatalogs publicCatalogs)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(electronicArtsApp);
        ArgumentNullException.ThrowIfNull(steam);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        this.lifetimeScope = lifetimeScope.BeginLifetimeScope(ConfigureLifetimeScope);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.appLifecycleManager = appLifecycleManager;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.electronicArtsApp = electronicArtsApp;
        this.steam = steam;
        this.superSnacks = superSnacks;
        this.blazorFramework = blazorFramework;
        this.publicCatalogs = publicCatalogs;
        enqueuedScanningTaskLock = new();
        enqueuedResamplingPacksTaskLock = new();
        enqueuedFresheningTaskLock = new();
        fileSystemWatcherConnectionLock = new();
        fresheningTaskLock = new();
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
    readonly IElectronicArtsApp electronicArtsApp;
    readonly AsyncLock enqueuedFresheningTaskLock;
    readonly AsyncLock enqueuedResamplingPacksTaskLock;
    readonly AsyncLock enqueuedScanningTaskLock;
    readonly StringComparison fileSystemStringComparison;
    readonly AsyncLock fileSystemWatcherConnectionLock;
    readonly AsyncLock fresheningTaskLock;
    Version? gameVersion;
    ImmutableArray<byte> globalModsManifestLastSha256 = ImmutableArray<byte>.Empty;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? installationDirectoryWatcher;
    IReadOnlyList<string> installedPackCodes = [];
    bool isScanning;
    bool isModsDisabledGameSettingOn;
    bool isScriptModsEnabledGameSettingOn = true;
    bool isShowModListStartupGameSettingOn;
    bool isSteamInstallation;
    string lastModHealthStatusSummary = string.Empty;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<ISmartSimObserver> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? packsDirectoryWatcher;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
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
                if (!EqualityComparer<Version>.Default.Equals(truncatedLastGameVersion, truncatedGameVersion))
                {
                    using var pbDbContext = pbDbContextFactory.CreateDbContext();
                    if (!IsModsDisabledGameSettingOn
                        && pbDbContext.ModFiles.Any(mf => mf.FileType == ModsDirectoryFileType.Package)
                        || IsScriptModsEnabledGameSettingOn
                        && pbDbContext.ModFiles.Any(mf => mf.FileType == ModsDirectoryFileType.ScriptArchive))
                    {
                        superSnacks.OfferRefreshments(new MarkupString(
                            $"""
                            You just successfully upgraded The Sims 4 from {truncatedLastGameVersion} to {truncatedGameVersion}! Well done! Would you like help joining a Discord server which announces updates for mods? There are probably at least a few important updates headed your way from creators.
                            """), Severity.Success, options =>
                            {
                                options.Icon = MaterialDesignIcons.Normal.TimelineCheck;
                                options.RequireInteraction = true;
                                var lifetimeScope = blazorFramework.MainLayoutLifetimeScope!;
                                var dialogService = lifetimeScope.Resolve<IDialogService>();
                                var publicCatalogs = lifetimeScope.Resolve<IPublicCatalogs>();
                                options.Onclick = async _ => await dialogService.ShowAskForHelpDialogAsync(logger, publicCatalogs, isPatchDay: true);
                            });
                        _ = Task.Run(async () => await platformFunctions.SendLocalNotificationAsync("Major Game Update Detected", "Please click here if you're interested in updating your mods for the new patch!"));
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
            FreshenGlobalManifest(force: true);
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

    bool CatalogIfInModsDirectory(string userDataDirectoryRelativePath, out bool wasGlobalManifestChange)
    {
        if (modsDirectoryRelativePathPattern.IsMatch(userDataDirectoryRelativePath))
        {
            PutCatalogerToBedIfGameIsRunning();
            var modsDirectoryRelativePath = userDataDirectoryRelativePath[5..];
            wasGlobalManifestChange = modsDirectoryRelativePath is GlobalModsManifestPackageName;
            if (!wasGlobalManifestChange)
                modsDirectoryCataloger.Catalog(modsDirectoryRelativePath);
            return !wasGlobalManifestChange;
        }
        wasGlobalManifestChange = false;
        return false;
    }

    bool CatalogIfModsDirectory(string userDataDirectoryRelativePath)
    {
        if (userDataDirectoryRelativePath == "Mods")
        {
            PutCatalogerToBedIfGameIsRunning();
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
    }

    public bool ClearCache()
    {
        try
        {
            foreach (var cacheComponent in cacheComponents)
            {
                cacheComponent.Refresh();
                if (cacheComponent.Exists)
                {
                    if (cacheComponent is DirectoryInfo directoryCacheComponent)
                        directoryCacheComponent.Delete(true);
                    else
                        cacheComponent.Delete();
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "a user-initiated attempt to clear the cache failed");
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a problem trying to clear your cache for you.<br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                Click me to start Guided Support for the PlumbBuddy Discord and we can get that log file looked at, if you want.
                """), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.EraserVariant;
                    options.Onclick = async _ =>
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
    }

    void ConnectToInstallationDirectory()
    {
        try
        {
            using var fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
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
                installationDirectoryWatcher = new FileSystemWatcher(settings.InstallationFolderPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter =
                          NotifyFilters.CreationTime
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.FileName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Size
                };
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

    async void ConnectToUserDataDirectory()
    {
        try
        {
            using var fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
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
            PutCatalogerToBedIfGameIsRunning();
            modsDirectoryCataloger.Catalog(string.Empty);
            FreshenGlobalManifest(force: true);
        }
    }

    void DisconnectFromInstallationDirectoryWatcher()
    {
        try
        {
            using var fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
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

    void DisconnectFromUserDataDirectoryWatcher()
    {
        try
        {
            using var fileSystemWatcherConnectionLockHeld = fileSystemWatcherConnectionLock.Lock(new CancellationToken(true));
            if (fileSystemWatcherConnectionLockHeld is null)
                return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (userDataDirectoryWatcher is not null)
        {
            var globalModsManifestPackageFile = new FileInfo(Path.Combine(userDataDirectoryWatcher.Path, "Mods", GlobalModsManifestPackageName));
            userDataDirectoryWatcher.Changed -= UserDataDirectoryFileSystemEntryChangedHandler;
            userDataDirectoryWatcher.Created -= UserDataDirectoryFileSystemEntryCreatedHandler;
            userDataDirectoryWatcher.Deleted -= UserDataDirectoryFileSystemEntryDeletedHandler;
            userDataDirectoryWatcher.Error -= UserDataDirectoryWatcherErrorHandler;
            userDataDirectoryWatcher.Renamed -= UserDataDirectoryFileSystemEntryRenamedHandler;
            userDataDirectoryWatcher.Dispose();
            userDataDirectoryWatcher = null;
            if (globalModsManifestPackageFile.Exists)
            {
                try
                {
                    globalModsManifestPackageFile.Delete();
                }
                catch (IOException)
                {
                }
                globalModsManifestLastSha256 = ImmutableArray<byte>.Empty;
            }
            cacheComponents = [];
            IsModsDisabledGameSettingOn = false;
            IsScriptModsEnabledGameSettingOn = true;
            IsShowModListStartupGameSettingOn = false;
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
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            lifetimeScope.Dispose();
        }
    }

    void FreshenGlobalManifest(bool force = false)
    {
        if (!settings.GenerateGlobalManifestPackage)
            return;
        _ = Task.Run(() => FreshenGlobalManifestAsync(force));
    }

    async Task FreshenGlobalManifestAsync(bool force = false)
    {
        var enqueuedFresheningTaskLockPotentiallyHeld = await enqueuedFresheningTaskLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
        if (enqueuedFresheningTaskLockPotentiallyHeld is null)
            return;
        using var fresheningTaskLockHeld = await fresheningTaskLock.LockAsync().ConfigureAwait(false);
        enqueuedFresheningTaskLockPotentiallyHeld.Dispose();
        var modsDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "Mods"));
        if (!modsDirectory.Exists)
            return;
        var globalModsManifestPackageFileInfo = new FileInfo(Path.Combine(modsDirectory.FullName, GlobalModsManifestPackageName));
        if (!force
            && globalModsManifestPackageFileInfo.Exists
            && globalModsManifestLastSha256.SequenceEqual(await ModFileManifestModel.GetFileSha256HashAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false)))
            return;
        var manifestedModFiles = new List<GlobalModsManifestModelManifestedModFile>();
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        foreach (var modFileHashElements in await pbDbContext.ModFileHashes
            .Where(mfh => mfh.ModFiles!.Any() && mfh.ModFileManifests!.Any())
            .Select(mfh => new
            {
                Paths = mfh.ModFiles!.Select(mf => mf.Path!).ToList(),
                Manifests = mfh.ModFileManifests!.Select(mfm => new
                {
                    mfm.Key,
                    mfm.TuningName,
                    CalculatedSha256 = mfm.CalculatedModFileManifestHash!.Sha256,
                    SubsumedSha256 = mfm.SubsumedHashes!.Select(mfmh => mfmh.Sha256).ToList()
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
        foreach (var manifestedModFile in manifestedModFiles.OrderBy(mfm => mfm.ModsFolderPath))
            model.ManifestedModFiles.Add(manifestedModFile);
        using var globalManifestPackage = new DataBasePackedFile();
        await globalManifestPackage.SetAsync(GlobalModsManifestModel.ResourceKey, model).ConfigureAwait(false);
        await globalManifestPackage.SaveAsAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false);
        globalModsManifestLastSha256 = await ModFileManifestModel.GetFileSha256HashAsync(globalModsManifestPackageFileInfo.FullName).ConfigureAwait(false);
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

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IModsDirectoryCataloger.State))
        {
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Cataloging)
                FreshenGlobalManifest(force: true);
            if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Cataloging or ModsDirectoryCatalogerState.Idle)
                Scan();
        }
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.CacheStatus) or nameof(ISettings.Type))
            Scan();
        else if (e.PropertyName is nameof(ISettings.GenerateGlobalManifestPackage))
        {
            var globalModsManifestPackageFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", GlobalModsManifestPackageName));
            if (!settings.GenerateGlobalManifestPackage && globalModsManifestPackageFile.Exists)
                globalModsManifestPackageFile.Delete();
            else if (settings.GenerateGlobalManifestPackage)
                FreshenGlobalManifest(force: true);
        }
        else if (e.PropertyName == nameof(ISettings.InstallationFolderPath))
        {
            DisconnectFromInstallationDirectoryWatcher();
            ConnectToInstallationDirectory();
        }
        else if (e.PropertyName == nameof(ISettings.Onboarded))
        {
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToInstallationDirectory();
            ConnectToUserDataDirectory();
        }
        else if (e.PropertyName == nameof(ISettings.UserDataFolderPath))
        {
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToUserDataDirectory();
        }
        else if ((e.PropertyName?.StartsWith("Scan", StringComparison.OrdinalIgnoreCase) ?? false) && settings.Onboarded)
            UpdateScanInitializationStatus();
    }

    public async Task HelpWithPackPurchaseAsync(string packCode, IDialogService dialogService, IReadOnlyList<string>? creators, string? electronicArtsPromoCode)
    {
        if (!IsSteamInstallation && creators?.Count is > 0 && electronicArtsPromoCode is not null)
        {
            if (await dialogService.ShowQuestionDialogAsync("An Opportunity Has Presented Itself...",
                    $"""
                    Electronic Arts has an affiliate program for creators which gives them a commission for sales of packs. It turns out that {creators.Humanize()} {(creators.Count is 1 ? "has" : "have")} a Promo Code for this program:
                    `{electronicArtsPromoCode}`<br />
                    Since you're interested in **{packCode}** because of how it works with this mod, would you like to support {creators.Humanize()} by entering this Promo Code at check-out?<br />
                    Doing so **will not cost you any more**, but it will cause {creators.Humanize()} to earn a commission on your purchase. If you want, I can copy it into your clipboard for you right now!
                    """,
                    true) is not { } copyToClipboardPreference)
                return;
            if (copyToClipboardPreference)
            {
                await Clipboard.SetTextAsync(electronicArtsPromoCode);
                await dialogService.ShowSuccessDialogAsync("Thanks for Supporting Creators! 🥰",
                    $"""
                    I've just copied this Promo Code to your computer's clipboard:
                    `{electronicArtsPromoCode}`<br />                    
                    You can now just paste it right in to the Promo Code field during check-out and help support {creators.Humanize()}!<br /><br />
                    <iframe src="https://giphy.com/embed/rlQgaKAzFj21hivpE8" width="480" height="360" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/blkbok-rlQgaKAzFj21hivpE8">via GIPHY</a></p>
                    """);
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
        await blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().ShowErrorDialogAsync("Umm, Whoops",
            $"""
            The Downloads folder I have on file for you does not exist:<br />
            `{settings.DownloadsFolderPath}`<br /><br />
            You may want to go into Settings and change that.
            """).ConfigureAwait(false);
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

    void PutCatalogerToBedIfGameIsRunning()
    {
        if (modsDirectoryCataloger.State is not ModsDirectoryCatalogerState.Sleeping
            && (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            || IsCacheLocked())
            Task.Run(PutCatalogerToBedWhileGameIsRunningAsync);
    }

    async Task PutCatalogerToBedWhileGameIsRunningAsync()
    {
        if (await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false) is { } ts4Process)
        {
            modsDirectoryCataloger.GoToSleep();
            await ts4Process.WaitForExitAsync().ConfigureAwait(false);
            ts4Process.Dispose();
            modsDirectoryCataloger.WakeUp();
        }
    }

    void ResampleCacheClarity()
    {
        foreach (var cacheComponent in cacheComponents)
            cacheComponent.Refresh();
        var anyCacheComponentsExistOnDisk = cacheComponents.Any(ce => ce.Exists);
        if (settings.CacheStatus is SmartSimCacheStatus.Clear && anyCacheComponentsExistOnDisk)
        {
            settings.CacheStatus = SmartSimCacheStatus.Normal;
            PutCatalogerToBedIfGameIsRunning();
        }
        else if (settings.CacheStatus is not SmartSimCacheStatus.Clear && !anyCacheComponentsExistOnDisk)
            settings.CacheStatus = SmartSimCacheStatus.Clear;
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
                superSnacks.OfferRefreshments(new MarkupString(
                    $"""
                    <h3>Whoops!</h3>
                    I ran into a problem trying to read the file which contains your game options for The Sims 4:<br />
                    <strong>{optionsIniFile.FullName}</strong><br />
                    <br />
                    Brief technical details:<br />
                    <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                    <br />
                    There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                    """), Severity.Warning, options =>
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

    async Task ResampleGameVersionAsync()
    {
        var normalizedInstallationDirectoryPath = Path.GetFullPath(settings.InstallationFolderPath);
        if (!Directory.Exists(normalizedInstallationDirectoryPath))
            return;
        if (await electronicArtsApp.GetTS4InstallationDirectoryAsync().ConfigureAwait(false) is { } eaInstallationDirectory
            && normalizedInstallationDirectoryPath == Path.GetFullPath(eaInstallationDirectory.FullName))
        {
            GameVersion = await electronicArtsApp.GetTS4InstallationVersionAsync().ConfigureAwait(false);
            return;
        }
        if (await steam.GetTS4InstallationDirectoryAsync().ConfigureAwait(false) is { } steamInstallationDirectory
            && normalizedInstallationDirectoryPath == Path.GetFullPath(steamInstallationDirectory.FullName))
        {
            GameVersion = await steam.GetTS4InstallationVersionAsync().ConfigureAwait(false);
            return;
        }
        GameVersion = null;
    }

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
        InstalledPackCodes = [.. packsDirectory.GetDirectories().Select(directoryInfo => directoryInfo.Name)];
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
            if (modsDirectoryCataloger.State is not (ModsDirectoryCatalogerState.Cataloging or ModsDirectoryCatalogerState.Idle))
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
                    modHealthStatusSummary = $"Click here to get help with the problem{(distinctAttentionWorthyScanIssueCaptions.Length is 1 ? string.Empty : "s")} that {distinctAttentionWorthyScanIssueCaptions.Humanize()}.";
                else
                    modHealthStatusSummary = $"Click here to get help with the problems that {distinctAttentionWorthyScanIssueCaptions.Take(2).Append("other issue".ToQuantity(distinctAttentionWorthyScanIssueCaptions.Length - 2, ShowQuantityAs.Words)).Humanize()}.";
                if (modHealthStatusSummary != lastModHealthStatusSummary)
                {
                    if (!appLifecycleManager.IsVisible)
                        await platformFunctions.SendLocalNotificationAsync("Your Sims 4 Setup Needs Attention", modHealthStatusSummary).ConfigureAwait(false);
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
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a show-stopping problem while trying to check the health of your Mods folder.<br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                More detailed technical information is available in my log.
                """), Severity.Error, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.BottleTonicPlus;
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
                if (initializationChange)
                    Scan();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unexpected exception encountered while initializing scans");
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a show-stopping problem while trying to check the health of your Mods folder.<br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                More detailed technical information is available in my log.
                """), Severity.Error, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.BottleTonicPlus;
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
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasOverwritten)
            FreshenGlobalManifest();
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
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasOverwritten)
            FreshenGlobalManifest();
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
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(relativePath, out var globalManifestWasDeleted))
            return;
        if (globalManifestWasDeleted)
            FreshenGlobalManifest();
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
            FreshenGlobalManifest();
            return;
        }
        if (CatalogIfInModsDirectory(oldRelativePath, out var globalManifestWasRenamed) | CatalogIfInModsDirectory(relativePath, out var globalManifestWasOverwritten))
            return;
        if (globalManifestWasRenamed || globalManifestWasOverwritten)
            FreshenGlobalManifest();
    }

    void UserDataDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromUserDataDirectoryWatcher();
        ConnectToUserDataDirectory();
    }
}
