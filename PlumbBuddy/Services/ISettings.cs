namespace PlumbBuddy.Services;

public interface ISettings :
    INotifyPropertyChanged
{
    bool AutomaticallyCheckForUpdates { get; set; }
    SmartSimCacheStatus CacheStatus { get; set; }
    bool DevToolsUnlocked { get; set; }
    string DownloadsFolderPath { get; set; }
    string InstallationFolderPath { get; set; }
    DateTimeOffset? LastCheckForUpdate { get; set; }
    bool Onboarded { get; set; }
    bool ScanForCacheStaleness { get; set; }
    bool ScanForErrorLogs { get; set; }
    bool ScanForLoose7ZipArchives { get; set; }
    bool ScanForLooseRarArchives { get; set; }
    bool ScanForLooseZipArchives { get; set; }
    bool ScanForMissingBe { get; set; }
    bool ScanForMissingDependency { get; set; }
    bool ScanForMissingMccc { get; set; }
    bool ScanForMissingModGuard { get; set; }
    bool ScanForMutuallyExclusiveMods { get; set; }
    bool ScanForInvalidModSubdirectoryDepth { get; set; }
    bool ScanForInvalidScriptModSubdirectoryDepth { get; set; }
    bool ScanForModsDisabled { get; set; }
    bool ScanForMultipleModVersions { get; set; }
    bool ScanForScriptModsDisabled { get; set; }
    bool ScanForShowModsListAtStartupEnabled { get; set; }
    bool ShowThemeManager { get; set; }
    string? Theme { get; set; }
    UserType Type { get; set; }
    bool UsePublicPackCatalog { get; set; }
    string UserDataFolderPath { get; set; }
    Version? VersionAtLastStartup { get; set; }
    bool WriteScaffoldingToSubdirectory { get; set; }

    void Forget();
}
