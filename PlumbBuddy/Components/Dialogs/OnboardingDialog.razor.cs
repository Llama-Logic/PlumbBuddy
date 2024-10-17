namespace PlumbBuddy.Components.Dialogs;

partial class OnboardingDialog
{
    FoldersSelector? foldersSelector;
    bool isLoading;
    string? loadingText;

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

    bool ScanForMutuallyExclusiveMods
    {
        get => Player.ScanForMutuallyExclusiveMods;
        set => Player.ScanForMutuallyExclusiveMods = value;
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

    bool ScanForShowModsListAtStartupEnabled
    {
        get => Player.ScanForShowModsListAtStartupEnabled;
        set => Player.ScanForShowModsListAtStartupEnabled = value;
    }

    UserType Type
    {
        get => Player.Type;
        set
        {
            Player.Type = value;
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
        if (targetIndex is >= 5)
        {
            Player.Onboarded = true;
            MudDialog?.Close(DialogResult.Ok(true));
            return false;
        }
        if (direction is StepChangeDirection.Backward)
            return false;
        switch (targetIndex)
        {
            case 3:
                return await ScanForFoldersAsync();
            case 4:
                var prevent = false;
                if (foldersSelector is not null)
                {
                    await foldersSelector.ValidateAsync();
                    prevent = !foldersSelector.IsValid;
                }
                return prevent;
        }
        return false;
    }

    void HandleUserPreferencesChanged(object? sender, PropertyChangedEventArgs e) =>
        StateHasChanged();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandleUserPreferencesChanged;
        SetDefaultScansForUserType(Player.Type);
    }

    async Task<bool> ScanForFoldersAsync()
    {
        if (foldersSelector is null)
            return true;
        if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
        {
            if (!await DialogService.ShowCautionDialogAsync("I may be about to spook your Mac",
                """
                It's awesome that you're using me on your Mac! It does *a lot* to keep you safe and one of those things is to stop programs from randomly going into your Documents folder. Trouble is, that's where your mods are (or will be), so I pretty much need to do that.<br />
                I'm going to poke in there now. If macOS pauses me to ask you if it's cool, please tell it that it's okay for me to be in there.<br />
                *Note: You can cancel this, but reading from this area on your computer is basically the reason I exist so I won't be able to continue without doing it.*
                """))
                return true;
            var randomDocumentsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", Guid.NewGuid().ToString("n"));
            using (var randomDocumentsFileStream = File.Open(randomDocumentsFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var randomDocumentsFileStreamWriter = new StreamWriter(randomDocumentsFileStream))
            {
                await randomDocumentsFileStreamWriter.WriteAsync("Hi!");
                await randomDocumentsFileStreamWriter.FlushAsync();
            }
            if (!File.Exists(randomDocumentsFile))
                return true;
            File.Delete(randomDocumentsFile);
        }
        loadingText = "☝️ Just a moment, I'm taking a looking at your computer...";
        isLoading = true;
        StateHasChanged();
        await foldersSelector.ScanForFoldersAsync();
        isLoading = false;
        StateHasChanged();
        return false;
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
                Player.ScanForMutuallyExclusiveMods = false;
                Player.ScanForShowModsListAtStartupEnabled = false;
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
                Player.ScanForMissingDependency = ScanAttribute.Get(typeof(IDependencyScan))?.IsEnabledByDefault ?? false;
                Player.ScanForCacheStaleness = ScanAttribute.Get(typeof(ICacheStalenessScan))?.IsEnabledByDefault ?? false;
                Player.ScanForResourceConflicts = ScanAttribute.Get(typeof(IResourceConflictScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMultipleModVersions = ScanAttribute.Get(typeof(IMultipleModVersionsScan))?.IsEnabledByDefault ?? false;
                Player.ScanForMutuallyExclusiveMods = ScanAttribute.Get(typeof(IExclusivityScan))?.IsEnabledByDefault ?? false;
                Player.ScanForShowModsListAtStartupEnabled = ScanAttribute.Get(typeof(IShowModListStartupSettingScan))?.IsEnabledByDefault ?? false;
                break;
        }
    }
}
