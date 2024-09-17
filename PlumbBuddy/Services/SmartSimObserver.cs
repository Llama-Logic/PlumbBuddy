namespace PlumbBuddy.Services;

public partial class SmartSimObserver :
    ISmartSimObserver
{
    [GeneratedRegex(@"^Mods[\\/].+$")]
    private static partial Regex GetModsDirectoryRelativePathPattern();

    [GeneratedRegex(@"^(?<path>.*?)[\\/]?$")]
    private static partial Regex GetTrimmedLocalPathSegmentsPattern();

    static readonly Regex modsDirectoryRelativePathPattern = GetModsDirectoryRelativePathPattern();
    static readonly Regex trimmedLocalPathSegmentsPattern = GetTrimmedLocalPathSegmentsPattern();

    public SmartSimObserver(ILifetimeScope lifetimeScope, ILogger<ISmartSimObserver> logger, IPlatformFunctions platformFunctions, IPlayer player, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.lifetimeScope = lifetimeScope.BeginLifetimeScope(ConfigureLifetimeScope);
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        fileSystemStringComparison = platformFunctions.FileSystemStringComparison;
        this.player.PropertyChanged += PlayerPropertyChangedHandler;
        ConnectToInstallationDirectory();
        ConnectToUserDataDirectory();
    }

    ~SmartSimObserver() =>
        Dispose(false);

    ImmutableArray<FileSystemInfo> cacheComponents;
    readonly StringComparison fileSystemStringComparison;
    FileSystemWatcher? installationDirectoryWatcher;
    bool isModsDisabledGameSettingOn;
    bool isScriptModsEnabledGameSettingOn;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<ISmartSimObserver> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    readonly ISuperSnacks superSnacks;
    FileSystemWatcher? userDataDirectoryWatcher;

    public bool IsModsDisabledGameSettingOn
    {
        get => isModsDisabledGameSettingOn;
        private set
        {
            isModsDisabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public bool IsScriptModsEnabledGameSettingOn
    {
        get => isScriptModsEnabledGameSettingOn;
        private set
        {
            isScriptModsEnabledGameSettingOn = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    bool CatalogIfInModsDirectory(string userDataDirectoryRelativePath)
    {
        if (modsDirectoryRelativePathPattern.IsMatch(userDataDirectoryRelativePath))
        {
            modsDirectoryCataloger.Catalog(userDataDirectoryRelativePath[5..]);
            return true;
        }
        return false;
    }

    public void ClearCache()
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
        }
        catch (Exception ex)
        {
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a problem trying to clear your cache for you.<br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                """), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.EraserVariant;
                });
        }
    }

    void ConfigureLifetimeScope(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<ModSettingScan>().As<IModSettingScan>();
        containerBuilder.RegisterType<ScriptModSettingScan>().As<IScriptModSettingScan>();
        containerBuilder.RegisterType<PackageDepthScan>().As<IPackageDepthScan>();
        containerBuilder.RegisterType<Ts4ScriptDepthScan>().As<ITs4ScriptDepthScan>();
        containerBuilder.RegisterType<LooseZipArchiveScan>().As<ILooseZipArchiveScan>();
        containerBuilder.RegisterType<LooseRarArchiveScan>().As<ILooseRarArchiveScan>();
        containerBuilder.RegisterType<Loose7ZipArchiveScan>().As<ILoose7ZipArchiveScan>();
        containerBuilder.RegisterType<ErrorLogScan>().As<IErrorLogScan>();
        containerBuilder.RegisterType<McccMissingScan>().As<IMcccMissingScan>();
        containerBuilder.RegisterType<BeMissingScan>().As<IBeMissingScan>();
        containerBuilder.RegisterType<ModGuardMissingScan>().As<IModGuardMissingScan>();
        containerBuilder.RegisterType<DependencyMissingScan>().As<IDependencyMissingScan>();
        containerBuilder.RegisterType<CacheStalenessScan>().As<ICacheStalenessScan>();
        containerBuilder.RegisterType<ResourceConflictScan>().As<IResourceConflictScan>();
        containerBuilder.RegisterType<MultipleModVersionsScan>().As<IMultipleModVersionsScan>();
    }

    void ConnectToInstallationDirectory()
    {
        if (player.Onboarded && Directory.Exists(player.InstallationFolderPath))
        {
            installationDirectoryWatcher = new FileSystemWatcher(player.InstallationFolderPath)
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
    }

    void ConnectToUserDataDirectory()
    {
        if (player.Onboarded && Directory.Exists(player.UserDataFolderPath))
        {
            Task.Run(ResampleGameOptionsAsync);
            cacheComponents =
            [
                new FileInfo(Path.Combine(player.UserDataFolderPath, "avatarcache.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "clientDB.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "houseDescription-client.package")),
                new FileInfo(Path.Combine(player.UserDataFolderPath, "localthumbcache.package")),
                new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "cachestr")),
                new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "onlinethumbnailcache"))
            ];
            ResampleCacheClarity();
            userDataDirectoryWatcher = new FileSystemWatcher(player.UserDataFolderPath)
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
            modsDirectoryCataloger.Catalog(string.Empty);
        }
    }

    void DisconnectFromInstallationDirectoryWatcher()
    {
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
    }

    void DisconnectFromUserDataDirectoryWatcher()
    {
        if (userDataDirectoryWatcher is not null)
        {
            userDataDirectoryWatcher.Changed -= UserDataDirectoryFileSystemEntryChangedHandler;
            userDataDirectoryWatcher.Created -= UserDataDirectoryFileSystemEntryCreatedHandler;
            userDataDirectoryWatcher.Deleted -= UserDataDirectoryFileSystemEntryDeletedHandler;
            userDataDirectoryWatcher.Error -= UserDataDirectoryWatcherErrorHandler;
            userDataDirectoryWatcher.Renamed -= UserDataDirectoryFileSystemEntryRenamedHandler;
            userDataDirectoryWatcher.Dispose();
            userDataDirectoryWatcher = null;
            cacheComponents = [];
            IsModsDisabledGameSettingOn = true;
            IsScriptModsEnabledGameSettingOn = false;
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
            player.PropertyChanged -= PlayerPropertyChangedHandler;
            lifetimeScope.Dispose();
        }
    }

    bool CatalogIfModsDirectory(string userDataDirectoryRelativePath)
    {
        if (userDataDirectoryRelativePath == "Mods")
        {
            modsDirectoryCataloger.Catalog("");
            return true;
        }
        return false;
    }

    string GetRelativePathInUserDataFolder(string fullPath)
    {
        var trimmedLocalPathSegmentsMatch = trimmedLocalPathSegmentsPattern.Match(player.UserDataFolderPath);
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

    void InstallationDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e)
    {
        // TODO: Phase 2 PreJector
    }

    void InstallationDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromInstallationDirectoryWatcher();
        ConnectToInstallationDirectory();
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public void OpenModsFolder()
    {
        if (player.Onboarded)
        {
            var modsDirectory = new DirectoryInfo(Path.Combine(player.UserDataFolderPath, "Mods"));
            if (modsDirectory.Exists)
                platformFunctions.ViewDirectory(modsDirectory);
        }
    }

    void PlayerPropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(player.InstallationFolderPath))
        {
            DisconnectFromInstallationDirectoryWatcher();
            ConnectToInstallationDirectory();
        }
        else if (e.PropertyName == nameof(player.Onboarded))
        {
            DisconnectFromInstallationDirectoryWatcher();
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToInstallationDirectory();
            ConnectToUserDataDirectory();
        }
        else if (e.PropertyName == nameof(player.UserDataFolderPath))
        {
            DisconnectFromUserDataDirectoryWatcher();
            ConnectToUserDataDirectory();
        }
    }

    void ResampleCacheClarity()
    {
        foreach (var cacheComponent in cacheComponents)
            cacheComponent.Refresh();
        var anyCacheComponentsExistOnDisk = cacheComponents.Any(ce => ce.Exists);
        if (player.CacheStatus is SmartSimCacheStatus.Clear && anyCacheComponentsExistOnDisk)
            player.CacheStatus = SmartSimCacheStatus.Normal;
        else if (player.CacheStatus is not SmartSimCacheStatus.Clear && !anyCacheComponentsExistOnDisk)
            player.CacheStatus = SmartSimCacheStatus.Clear;
    }

    async Task ResampleGameOptionsAsync()
    {
        var parsedSuccessfully = false;
        var optionsIniFile = new FileInfo(Path.Combine(player.UserDataFolderPath, "Options.ini"));
        if (optionsIniFile.Exists)
        {
            try
            {
                var parser = new IniDataParser();
                var data = parser.Parse(await File.ReadAllTextAsync(optionsIniFile.FullName).ConfigureAwait(false));
                var optionsData = data["options"];
                IsModsDisabledGameSettingOn = optionsData["modsdisabled"] == "1";
                IsScriptModsEnabledGameSettingOn = optionsData["scriptmodsenabled"] == "1";
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
        }
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

    void UserDataDirectoryFileSystemEntryChangedHandler(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfInModsDirectory(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryCreatedHandler(object sender, FileSystemEventArgs e)
    {
        if (player.CacheStatus is SmartSimCacheStatus.Clear && cacheComponents.Any(cc => e.FullPath.StartsWith(cc.FullName, fileSystemStringComparison)))
        {
            player.CacheStatus = SmartSimCacheStatus.Normal;
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(relativePath))
            return;
        if (CatalogIfInModsDirectory(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryDeletedHandler(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);
        if (player.CacheStatus is not SmartSimCacheStatus.Clear && cacheComponents.Any(cc => Path.GetFullPath(cc.FullName).Equals(fullPath, fileSystemStringComparison)))
        {
            ResampleCacheClarity();
            return;
        }
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(relativePath))
            return;
        if (CatalogIfInModsDirectory(relativePath))
            return;
    }

    void UserDataDirectoryFileSystemEntryRenamedHandler(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = GetRelativePathInUserDataFolder(e.OldFullPath);
        var relativePath = GetRelativePathInUserDataFolder(e.FullPath);
        if (ResampleGameOptionsIfTheyChanged(oldRelativePath) | ResampleGameOptionsIfTheyChanged(relativePath))
            return;
        if (CatalogIfModsDirectory(oldRelativePath) | CatalogIfModsDirectory(relativePath))
            return;
        if (CatalogIfInModsDirectory(oldRelativePath) | CatalogIfInModsDirectory(relativePath))
            return;
    }

    void UserDataDirectoryWatcherErrorHandler(object sender, ErrorEventArgs e)
    {
        DisconnectFromUserDataDirectoryWatcher();
        ConnectToUserDataDirectory();
    }
}
