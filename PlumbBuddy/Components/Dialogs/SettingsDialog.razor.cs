namespace PlumbBuddy.Components.Dialogs;

partial class SettingsDialog
{
    FoldersSelector? foldersSelector;
    MudTabs? tabs;
    ThemeSelector? themeSelector;

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

    bool ScanForResourceConflicts { get; set; }

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
            InstallationFolderPath = Player.InstallationFolderPath;
            UserDataFolderPath = Player.UserDataFolderPath;
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        ScanForCacheStaleness = Player.ScanForCacheStaleness;
        ScanForErrorLogs = Player.ScanForErrorLogs;
        ScanForInvalidModSubdirectoryDepth = Player.ScanForInvalidModSubdirectoryDepth;
        ScanForInvalidScriptModSubdirectoryDepth = Player.ScanForInvalidScriptModSubdirectoryDepth;
        ScanForLoose7ZipArchives = Player.ScanForLoose7ZipArchives;
        ScanForLooseRarArchives = Player.ScanForLooseRarArchives;
        ScanForLooseZipArchives = Player.ScanForLooseZipArchives;
        ScanForMissingBe = Player.ScanForMissingBe;
        ScanForMissingDependency = Player.ScanForMissingDependency;
        ScanForMissingMccc = Player.ScanForMissingMccc;
        ScanForMissingModGuard = Player.ScanForMissingModGuard;
        ScanForModsDisabled = Player.ScanForModsDisabled;
        ScanForMultipleModVersions = Player.ScanForMultipleModVersions;
        ScanForMutuallyExclusiveMods = Player.ScanForMutuallyExclusiveMods;
        ScanForResourceConflicts = Player.ScanForResourceConflicts;
        ScanForScriptModsDisabled = Player.ScanForScriptModsDisabled;
        ScanForShowModsListAtStartupEnabled = Player.ScanForShowModsListAtStartupEnabled;
        Type = Player.Type;
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
        Player.InstallationFolderPath = InstallationFolderPath;
        Player.ScanForCacheStaleness = ScanForCacheStaleness;
        Player.ScanForErrorLogs = ScanForErrorLogs;
        Player.ScanForInvalidModSubdirectoryDepth = ScanForInvalidModSubdirectoryDepth;
        Player.ScanForInvalidScriptModSubdirectoryDepth = ScanForInvalidScriptModSubdirectoryDepth;
        Player.ScanForLoose7ZipArchives = ScanForLoose7ZipArchives;
        Player.ScanForLooseRarArchives = ScanForLooseRarArchives;
        Player.ScanForLooseZipArchives = ScanForLooseZipArchives;
        Player.ScanForMissingBe = ScanForMissingBe;
        Player.ScanForMissingDependency = ScanForMissingDependency;
        Player.ScanForMissingMccc = ScanForMissingMccc;
        Player.ScanForMissingModGuard = ScanForMissingModGuard;
        Player.ScanForModsDisabled = ScanForModsDisabled;
        Player.ScanForMultipleModVersions = ScanForMultipleModVersions;
        Player.ScanForMutuallyExclusiveMods = ScanForMutuallyExclusiveMods;
        Player.ScanForResourceConflicts = ScanForResourceConflicts;
        Player.ScanForScriptModsDisabled = ScanForScriptModsDisabled;
        Player.ScanForShowModsListAtStartupEnabled = ScanForShowModsListAtStartupEnabled;
        Player.Type = Type;
        Player.UserDataFolderPath = UserDataFolderPath;
        MudDialog?.Close(DialogResult.Ok(true));
    }
}
