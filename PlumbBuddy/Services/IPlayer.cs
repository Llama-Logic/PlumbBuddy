namespace PlumbBuddy.Services;

public interface IPlayer :
    INotifyPropertyChanged
{
    SmartSimCacheStatus CacheStatus { get; set; }
    bool DevToolsUnlocked { get; set; }
    string InstallationFolderPath { get; set; }
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
    bool ScanForInvalidModSubdirectoryDepth { get; set; }
    bool ScanForInvalidScriptModSubdirectoryDepth { get; set; }
    bool ScanForModsDisabled { get; set; }
    bool ScanForMultipleModVersions { get; set; }
    bool ScanForResourceConflicts { get; set; }
    bool ScanForScriptModsDisabled { get; set; }
    bool ShowThemeManager { get; set; }
    UserType Type { get; set; }
    string UserDataFolderPath { get; set; }

    void Forget();
}
