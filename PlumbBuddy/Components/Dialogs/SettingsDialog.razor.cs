namespace PlumbBuddy.Components.Dialogs;

partial class SettingsDialog
{
    IReadOnlyList<string> defaultCreators = [];
    ChipSetField? defaultCreatorsChipSetField;
    FoldersSelector? foldersSelector;
    IReadOnlyList<string> modHoundPackagesExclusions = [];
    ChipSetField? modHoundPackagesExclusionsChipSetField;
    MudTabs? tabs;
    ThemeSelector? themeSelector;
    UserType type;

    [Parameter]
    public int ActivePanelIndex { get; set; }

    string ArchiveFolderPath { get; set; } = string.Empty;

    bool AutomaticallyCheckForUpdates { get; set; }

    string DownloadsFolderPath { get; set; } = string.Empty;

    bool ForceGameProcessPerformanceProcessorAffinity { get; set; }

    bool GenerateGlobalManifestPackage { get; set; }

    string InstallationFolderPath { get; set; } = string.Empty;

    bool OfferPatchDayModUpdatesHelp { get; set; }

    ModHoundExcludePackagesMode ModHoundExcludePackagesMode { get; set; }

    long? ModHoundReportRetentionPeriodTicks { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    bool ScanForCacheStaleness { get; set; }

    bool ScanForCorruptMods { get; set; }

    bool ScanForCorruptScriptMods { get; set; }

    bool ScanForErrorLogs { get; set; }

    bool ScanForInvalidModSubdirectoryDepth { get; set; }

    bool ScanForInvalidScriptModSubdirectoryDepth { get; set; }

    bool ScanForLoose7ZipArchives { get; set; }

    bool ScanForLooseRarArchives { get; set; }

    bool ScanForLooseZipArchives { get; set; }

    bool ScanForMismatchedInscribedHashes { get; set; }

    bool ScanForMissingBe { get; set; }

    bool ScanForMissingDependency { get; set; }

    bool ScanForMissingMccc { get; set; }

    bool ScanForMissingModGuard { get; set; }

    bool ScanForMultipleModVersions { get; set; }

    bool ScanForMutuallyExclusiveMods { get; set; }

    bool ScanForModsDisabled { get; set; }

    bool ScanForScriptModsDisabled { get; set; }

    bool ScanForShowModsListAtStartupEnabled { get; set; }

    bool ShowSystemTrayIcon { get; set; }

    UserType Type
    {
        get => type;
        set
        {
            type = value;
            if (type is UserType.Casual && ModHoundExcludePackagesMode is ModHoundExcludePackagesMode.Patterns)
            {
                ModHoundExcludePackagesMode = ModHoundExcludePackagesMode.StartsWith;
            }
        }
    }

    string UserDataFolderPath { get; set; } = string.Empty;

    void HandleSetModHoundReportRetentionPeriodDefault() =>
        ModHoundReportRetentionPeriodTicks = 4L * 7 * 24 * 60 * 60 * 10_000_000;

    void HandleSetModHoundReportRetentionPeriodIndefinite() =>
        ModHoundReportRetentionPeriodTicks = null;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            defaultCreators = [..Settings.DefaultCreatorsList.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
            modHoundPackagesExclusions = Settings.ModHoundPackagesExclusions;
            if (foldersSelector is not null)
            {
                await foldersSelector.ScanForFoldersAsync();
                ArchiveFolderPath = Settings.ArchiveFolderPath;
                DownloadsFolderPath = Settings.DownloadsFolderPath;
                InstallationFolderPath = Settings.InstallationFolderPath;
                UserDataFolderPath = Settings.UserDataFolderPath;
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        AutomaticallyCheckForUpdates = Settings.AutomaticallyCheckForUpdates;
        ForceGameProcessPerformanceProcessorAffinity = Settings.ForceGameProcessPerformanceProcessorAffinity;
        GenerateGlobalManifestPackage = Settings.GenerateGlobalManifestPackage;
        ModHoundExcludePackagesMode = Settings.ModHoundExcludePackagesMode;
        ModHoundReportRetentionPeriodTicks = Settings.ModHoundReportRetentionPeriod?.Ticks;
        OfferPatchDayModUpdatesHelp = Settings.OfferPatchDayModUpdatesHelp;
        ScanForCacheStaleness = Settings.ScanForCacheStaleness;
        ScanForCorruptMods = Settings.ScanForCorruptMods;
        ScanForCorruptScriptMods = Settings.ScanForCorruptScriptMods;
        ScanForErrorLogs = Settings.ScanForErrorLogs;
        ScanForInvalidModSubdirectoryDepth = Settings.ScanForInvalidModSubdirectoryDepth;
        ScanForInvalidScriptModSubdirectoryDepth = Settings.ScanForInvalidScriptModSubdirectoryDepth;
        ScanForLoose7ZipArchives = Settings.ScanForLoose7ZipArchives;
        ScanForLooseRarArchives = Settings.ScanForLooseRarArchives;
        ScanForLooseZipArchives = Settings.ScanForLooseZipArchives;
        ScanForMismatchedInscribedHashes = Settings.ScanForMismatchedInscribedHashes;
        ScanForMissingBe = Settings.ScanForMissingBe;
        ScanForMissingDependency = Settings.ScanForMissingDependency;
        ScanForMissingMccc = Settings.ScanForMissingMccc;
        ScanForMissingModGuard = Settings.ScanForMissingModGuard;
        ScanForModsDisabled = Settings.ScanForModsDisabled;
        ScanForMultipleModVersions = Settings.ScanForMultipleModVersions;
        ScanForMutuallyExclusiveMods = Settings.ScanForMutuallyExclusiveMods;
        ScanForScriptModsDisabled = Settings.ScanForScriptModsDisabled;
        ScanForShowModsListAtStartupEnabled = Settings.ScanForShowModsListAtStartupEnabled;
        ShowSystemTrayIcon = Settings.ShowSystemTrayIcon;
        Type = Settings.Type;
    }

    void CancelOnClickHandler()
    {
        themeSelector?.Cancel();
        MudDialog?.Close(DialogResult.Cancel());
    }

    async Task OkOnClickHandlerAsync()
    {
        if (foldersSelector is not null && tabs is not null)
        {
            await foldersSelector.ValidateAsync();
            if (!foldersSelector.IsValid)
            {
                tabs.ActivatePanel(2);
                return;
            }
        }
        if (defaultCreatorsChipSetField is not null)
            await defaultCreatorsChipSetField.CommitPendingEntryIfEmptyAsync();
        Settings.ArchiveFolderPath = ArchiveFolderPath;
        Settings.AutomaticallyCheckForUpdates = AutomaticallyCheckForUpdates;
        Settings.DefaultCreatorsList = string.Join(Environment.NewLine, defaultCreators);
        Settings.DownloadsFolderPath = DownloadsFolderPath;
        Settings.ForceGameProcessPerformanceProcessorAffinity = ForceGameProcessPerformanceProcessorAffinity;
        Settings.GenerateGlobalManifestPackage = GenerateGlobalManifestPackage;
        Settings.InstallationFolderPath = InstallationFolderPath;
        Settings.ModHoundExcludePackagesMode = ModHoundExcludePackagesMode;
        Settings.ModHoundPackagesExclusions = modHoundPackagesExclusions.ToArray();
        Settings.ModHoundReportRetentionPeriod = ModHoundReportRetentionPeriodTicks is { } ticks ? new(ticks) : null;
        Settings.OfferPatchDayModUpdatesHelp = OfferPatchDayModUpdatesHelp;
        Settings.ScanForCacheStaleness = ScanForCacheStaleness;
        Settings.ScanForCorruptMods = ScanForCorruptMods;
        Settings.ScanForCorruptScriptMods = ScanForCorruptScriptMods;
        Settings.ScanForErrorLogs = ScanForErrorLogs;
        Settings.ScanForInvalidModSubdirectoryDepth = ScanForInvalidModSubdirectoryDepth;
        Settings.ScanForInvalidScriptModSubdirectoryDepth = ScanForInvalidScriptModSubdirectoryDepth;
        Settings.ScanForLoose7ZipArchives = ScanForLoose7ZipArchives;
        Settings.ScanForLooseRarArchives = ScanForLooseRarArchives;
        Settings.ScanForLooseZipArchives = ScanForLooseZipArchives;
        Settings.ScanForMismatchedInscribedHashes = ScanForMismatchedInscribedHashes;
        Settings.ScanForMissingBe = ScanForMissingBe;
        Settings.ScanForMissingDependency = ScanForMissingDependency;
        Settings.ScanForMissingMccc = ScanForMissingMccc;
        Settings.ScanForMissingModGuard = ScanForMissingModGuard;
        Settings.ScanForModsDisabled = ScanForModsDisabled;
        Settings.ScanForMultipleModVersions = ScanForMultipleModVersions;
        Settings.ScanForMutuallyExclusiveMods = ScanForMutuallyExclusiveMods;
        Settings.ScanForScriptModsDisabled = ScanForScriptModsDisabled;
        Settings.ScanForShowModsListAtStartupEnabled = ScanForShowModsListAtStartupEnabled;
        Settings.ShowSystemTrayIcon = ShowSystemTrayIcon;
        Settings.Type = Type;
        Settings.UserDataFolderPath = UserDataFolderPath;
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
