namespace PlumbBuddy.Services;

public interface ISettings :
    INotifyPropertyChanged
{
    string ArchiveFolderPath { get; set; }
    bool ArchivistEnabled { get; set; }
    bool ArchivistAutoIngestSaves { get; set; }
    bool AutomaticallyCheckForUpdates { get; set; }
    SmartSimCacheStatus CacheStatus { get; set; }
    bool ConnectToGamePads { get; set; }
    string DefaultCreatorsList { get; set; }
    bool DevToolsUnlocked { get; set; }
    string DownloadsFolderPath { get; set; }
    bool ForceGameProcessPerformanceProcessorAffinity { get; set; }
    bool GenerateGlobalManifestPackage { get; set; }
    string InstallationFolderPath { get; set; }
    DateTimeOffset? LastCheckForUpdate { get; set; }
    Version? LastGameVersion { get; set; }
    ModHoundExcludePackagesMode ModHoundExcludePackagesMode { get; set; }
    ImmutableArray<string> ModHoundPackagesExclusions { get; set; }
    TimeSpan? ModHoundReportRetentionPeriod { get; set; }
    bool OfferPatchDayModUpdatesHelp { get; set; }
    bool Onboarded { get; set; }
    string ParlayName { get; set; }
    bool ScanForCacheStaleness { get; set; }
    bool ScanForCorruptMods { get; set; }
    bool ScanForCorruptScriptMods { get; set; }
    bool ScanForErrorLogs { get; set; }
    bool ScanForLoose7ZipArchives { get; set; }
    bool ScanForLooseRarArchives { get; set; }
    bool ScanForLooseZipArchives { get; set; }
    bool ScanForMismatchedInscribedHashes { get; set; }
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
    bool ScanForWrongGameVersion { get; set; }
    bool ScanForWrongGameVersionSC { get; set; }
    bool ScanForWrongGameVersionTS2 { get; set; }
    bool ScanForWrongGameVersionTS3 { get; set; }
    bool ShowDlcRetailUsd { get; set; }
    bool ShowSystemTrayIcon { get; set; }
    bool ShowThemeManager { get; set; }
    Version? SkipUpdateVersion { get; set; }
    string? Theme { get; set; }
    UserType Type { get; set; }
    bool UsePublicPackCatalog { get; set; }
    string UserDataFolderPath { get; set; }
    Version? VersionAtLastStartup { get; set; }
    bool WriteScaffoldingToSubdirectory { get; set; }
    decimal UiZoom { get; set; }

    void Forget();
}
