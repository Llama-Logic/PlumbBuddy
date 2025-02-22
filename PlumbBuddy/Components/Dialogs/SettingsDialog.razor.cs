namespace PlumbBuddy.Components.Dialogs;

partial class SettingsDialog
{
    IReadOnlyList<string> defaultCreators = [];
    ChipSetField? defaultCreatorsChipSetField;
    FoldersSelector? foldersSelector;
    MudTabs? tabs;
    ThemeSelector? themeSelector;

    string ArchiveFolderPath { get; set; } = string.Empty;

    bool AutomaticallyCheckForUpdates { get; set; }

    string DownloadsFolderPath { get; set; } = string.Empty;

    bool ForceGameProcessPerformanceProcessorAffinity { get; set; }

    bool GenerateGlobalManifestPackage { get; set; }

    string InstallationFolderPath { get; set; } = string.Empty;

    bool OfferPatchDayModUpdatesHelp { get; set; }

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

    UserType Type { get; set; }

    string UserDataFolderPath { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            defaultCreators = [..Settings.DefaultCreatorsList.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
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
