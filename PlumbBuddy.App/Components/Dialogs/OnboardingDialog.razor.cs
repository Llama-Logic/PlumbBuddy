namespace PlumbBuddy.App.Components.Dialogs;

partial class OnboardingDialog
{
    FoldersSelector? foldersSelector;
    bool isLoading;
    string? loadingText;
    bool seenScansToggler;

    string InstallationFolderPath
    {
        get => Player.InstallationFolderPath;
        set => Player.InstallationFolderPath = value;
    }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    bool ScanForCacheStaleness
    {
        get => Player.ScanForCacheStaleness;
        set => Player.ScanForCacheStaleness = value;
    }

    bool ScanForErrorLogs
    {
        get => Player.ScanForErrorLogs;
        set => Player.ScanForErrorLogs = value;
    }

    bool ScanForInvalidModSubdirectoryDepth
    {
        get => Player.ScanForInvalidModSubdirectoryDepth;
        set => Player.ScanForInvalidModSubdirectoryDepth = value;
    }

    bool ScanForInvalidScriptModSubdirectoryDepth
    {
        get => Player.ScanForInvalidScriptModSubdirectoryDepth;
        set => Player.ScanForInvalidScriptModSubdirectoryDepth = value;
    }

    bool ScanForLoose7ZipArchives
    {
        get => Player.ScanForLoose7ZipArchives;
        set => Player.ScanForLoose7ZipArchives = value;
    }

    bool ScanForLooseRarArchives
    {
        get => Player.ScanForLooseRarArchives;
        set => Player.ScanForLooseRarArchives = value;
    }

    bool ScanForLooseZipArchives
    {
        get => Player.ScanForLooseZipArchives;
        set => Player.ScanForLooseZipArchives = value;
    }

    bool ScanForMissingBe
    {
        get => Player.ScanForMissingBe;
        set => Player.ScanForMissingBe = value;
    }

    bool ScanForMissingDependency
    {
        get => Player.ScanForMissingDependency;
        set => Player.ScanForMissingDependency = value;
    }

    bool ScanForMissingMccc
    {
        get => Player.ScanForMissingMccc;
        set => Player.ScanForMissingMccc = value;
    }

    bool ScanForMissingModGuard
    {
        get => Player.ScanForMissingModGuard;
        set => Player.ScanForMissingModGuard = value;
    }

    bool ScanForMultipleModVersions
    {
        get => Player.ScanForMultipleModVersions;
        set => Player.ScanForMultipleModVersions = value;
    }

    bool ScanForModsDisabled
    {
        get => Player.ScanForModsDisabled;
        set => Player.ScanForModsDisabled = value;
    }

    bool ScanForResourceConflicts
    {
        get => Player.ScanForResourceConflicts;
        set => Player.ScanForResourceConflicts = value;
    }

    bool ScanForScriptModsDisabled
    {
        get => Player.ScanForScriptModsDisabled;
        set => Player.ScanForScriptModsDisabled = value;
    }

    UserType Type
    {
        get => Player.Type;
        set
        {
            Player.Type = value;
            if (!seenScansToggler)
                SetDefaultScansForUserType(value);
        }
    }

    string UserDataFolderPath
    {
        get => Player.UserDataFolderPath;
        set => Player.UserDataFolderPath = value;
    }

    /// <inheritdoc />
    public void Dispose() =>
        Player.PropertyChanged -= HandleUserPreferencesChanged;

    async Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (targetIndex is >= 3)
        {
            Player.Onboarded = true;
            MudDialog?.Close(DialogResult.Ok(true));
            return false;
        }
        if (direction is StepChangeDirection.Backward)
            return false;
        switch (targetIndex)
        {
            case 1:
                await ScanForFoldersAsync();
                return false;
            case 2:
                var prevent = false;
                if (foldersSelector is not null)
                {
                    await foldersSelector.ValidateAsync();
                    prevent = !foldersSelector.IsValid;
                }
                if (!prevent)
                    seenScansToggler = true;
                return prevent;
        }
        return false;
    }

    void HandleUserPreferencesChanged(object? sender, PropertyChangedEventArgs e) =>
        StateHasChanged();

    Task LearnMoreAboutDiscordOnClickHandlerAsync() =>
        Launcher.OpenAsync("https://discord.com/");

    Task LearnMoreAboutMcccOnClickHandlerAsync() =>
        Launcher.OpenAsync("https://deaderpool-mccc.com/");

    Task LearnMoreAboutModGuardOnClickHandlerAsync() =>
        Launcher.OpenAsync("https://www.patreon.com/posts/98126153");

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandleUserPreferencesChanged;
        SetDefaultScansForUserType(Player.Type);
    }

    async Task ScanForFoldersAsync()
    {
        if (foldersSelector is null)
            return;
        loadingText = "☝️ Just a moment, I'm taking a looking at your computer...";
        isLoading = true;
        StateHasChanged();
        await foldersSelector.ScanForFoldersAsync();
        isLoading = false;
        StateHasChanged();
    }

    void SetDefaultScansForUserType(UserType value)
    {
        switch (value)
        {
            case UserType.Creator:
                Player.ScanForModsDisabled = false;
                Player.ScanForScriptModsDisabled = false;
                Player.ScanForInvalidModSubdirectoryDepth = false;
                Player.ScanForInvalidScriptModSubdirectoryDepth = false;
                Player.ScanForLooseZipArchives = false;
                Player.ScanForLooseRarArchives = false;
                Player.ScanForLoose7ZipArchives = false;
                Player.ScanForErrorLogs = false;
                Player.ScanForMissingMccc = false;
                Player.ScanForMissingBe = false;
                Player.ScanForMissingModGuard = false;
                Player.ScanForMissingDependency = false;
                Player.ScanForCacheStaleness = false;
                Player.ScanForResourceConflicts = false;
                Player.ScanForMultipleModVersions = false;
                break;
            default:
                Player.ScanForModsDisabled = ScanAttribute.Get(typeof(IModSettingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForScriptModsDisabled = ScanAttribute.Get(typeof(IScriptModSettingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForInvalidModSubdirectoryDepth = ScanAttribute.Get(typeof(IPackageDepthScan))?.IsEnabledByDefault ?? false;
                Player.ScanForInvalidScriptModSubdirectoryDepth = ScanAttribute.Get(typeof(ITs4ScriptDepthScan))?.IsEnabledByDefault ?? false;
                Player.ScanForLooseZipArchives = ScanAttribute.Get(typeof(ILooseZipArchiveScan))?.IsEnabledByDefault ?? false;
                Player.ScanForLooseRarArchives = ScanAttribute.Get(typeof(ILooseRarArchiveScan))?.IsEnabledByDefault ?? false;
                Player.ScanForLoose7ZipArchives = ScanAttribute.Get(typeof(ILoose7ZipArchiveScan))?.IsEnabledByDefault ?? false;
                Player.ScanForErrorLogs = ScanAttribute.Get(typeof(IErrorLogScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMissingMccc = ScanAttribute.Get(typeof(IMcccMissingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMissingBe = ScanAttribute.Get(typeof(IBeMissingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMissingModGuard = ScanAttribute.Get(typeof(IModGuardMissingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMissingDependency = ScanAttribute.Get(typeof(IDependencyMissingScan))?.IsEnabledByDefault ?? false;
                Player.ScanForCacheStaleness = ScanAttribute.Get(typeof(ICacheStalenessScan))?.IsEnabledByDefault ?? false;
                Player.ScanForResourceConflicts = ScanAttribute.Get(typeof(IResourceConflictScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMultipleModVersions = ScanAttribute.Get(typeof(IMultipleModVersionsScan))?.IsEnabledByDefault ?? false;
                break;
        }
    }
}
