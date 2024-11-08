namespace PlumbBuddy.Components.Dialogs;

partial class SettingsDialog
{
    FoldersSelector? foldersSelector;
    MudTabs? tabs;
    ThemeSelector? themeSelector;

    bool AutomaticallyCheckForUpdates { get; set; }

    string DownloadsFolderPath { get; set; } = string.Empty;

    string InstallationFolderPath { get; set; } = string.Empty;

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    bool ScanForCacheStaleness { get; set; }

    bool ScanForErrorLogs { get; set; }

    bool ScanForInvalidModSubdirectoryDepth { get; set; }

    bool ScanForInvalidScriptModSubdirectoryDepth { get; set; }

    bool ScanForLoose7ZipArchives { get; set; }

    bool ScanForLooseRarArchives { get; set; }

    bool ScanForLooseZipArchives { get; set; }

    bool ScanForMissingBe { get; set; }

    bool ScanForMissingDependency { get; set; }

    bool ScanForMissingMccc { get; set; }

    bool ScanForMissingModGuard { get; set; }

    bool ScanForMultipleModVersions { get; set; }

    bool ScanForMutuallyExclusiveMods { get; set; }

    bool ScanForModsDisabled { get; set; }

    bool ScanForScriptModsDisabled { get; set; }

    bool ScanForShowModsListAtStartupEnabled { get; set; }

    UserType Type { get; set; }

    string UserDataFolderPath { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && foldersSelector is not null)
        {
            await foldersSelector.ScanForFoldersAsync();
            DownloadsFolderPath = Settings.DownloadsFolderPath;
            InstallationFolderPath = Settings.InstallationFolderPath;
            UserDataFolderPath = Settings.UserDataFolderPath;
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        AutomaticallyCheckForUpdates = Settings.AutomaticallyCheckForUpdates;
        ScanForCacheStaleness = Settings.ScanForCacheStaleness;
        ScanForErrorLogs = Settings.ScanForErrorLogs;
        ScanForInvalidModSubdirectoryDepth = Settings.ScanForInvalidModSubdirectoryDepth;
        ScanForInvalidScriptModSubdirectoryDepth = Settings.ScanForInvalidScriptModSubdirectoryDepth;
        ScanForLoose7ZipArchives = Settings.ScanForLoose7ZipArchives;
        ScanForLooseRarArchives = Settings.ScanForLooseRarArchives;
        ScanForLooseZipArchives = Settings.ScanForLooseZipArchives;
        ScanForMissingBe = Settings.ScanForMissingBe;
        ScanForMissingDependency = Settings.ScanForMissingDependency;
        ScanForMissingMccc = Settings.ScanForMissingMccc;
        ScanForMissingModGuard = Settings.ScanForMissingModGuard;
        ScanForModsDisabled = Settings.ScanForModsDisabled;
        ScanForMultipleModVersions = Settings.ScanForMultipleModVersions;
        ScanForMutuallyExclusiveMods = Settings.ScanForMutuallyExclusiveMods;
        ScanForScriptModsDisabled = Settings.ScanForScriptModsDisabled;
        ScanForShowModsListAtStartupEnabled = Settings.ScanForShowModsListAtStartupEnabled;
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
                tabs.ActivatePanel(1);
                return;
            }
        }
        Settings.AutomaticallyCheckForUpdates = AutomaticallyCheckForUpdates;
        Settings.DownloadsFolderPath = DownloadsFolderPath;
        Settings.InstallationFolderPath = InstallationFolderPath;
        Settings.ScanForCacheStaleness = ScanForCacheStaleness;
        Settings.ScanForErrorLogs = ScanForErrorLogs;
        Settings.ScanForInvalidModSubdirectoryDepth = ScanForInvalidModSubdirectoryDepth;
        Settings.ScanForInvalidScriptModSubdirectoryDepth = ScanForInvalidScriptModSubdirectoryDepth;
        Settings.ScanForLoose7ZipArchives = ScanForLoose7ZipArchives;
        Settings.ScanForLooseRarArchives = ScanForLooseRarArchives;
        Settings.ScanForLooseZipArchives = ScanForLooseZipArchives;
        Settings.ScanForMissingBe = ScanForMissingBe;
        Settings.ScanForMissingDependency = ScanForMissingDependency;
        Settings.ScanForMissingMccc = ScanForMissingMccc;
        Settings.ScanForMissingModGuard = ScanForMissingModGuard;
        Settings.ScanForModsDisabled = ScanForModsDisabled;
        Settings.ScanForMultipleModVersions = ScanForMultipleModVersions;
        Settings.ScanForMutuallyExclusiveMods = ScanForMutuallyExclusiveMods;
        Settings.ScanForScriptModsDisabled = ScanForScriptModsDisabled;
        Settings.ScanForShowModsListAtStartupEnabled = ScanForShowModsListAtStartupEnabled;
        Settings.Type = Type;
        Settings.UserDataFolderPath = UserDataFolderPath;
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
