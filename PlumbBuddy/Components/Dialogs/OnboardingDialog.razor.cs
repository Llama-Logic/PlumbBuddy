namespace PlumbBuddy.Components.Dialogs;

partial class OnboardingDialog
{
    IReadOnlyList<string> defaultCreators = [];
    ChipSetField? defaultCreatorsChipSetField;
    FoldersSelector? foldersSelector;
    bool isLoading;
    string? loadingText;

    bool AutomaticallyCheckForUpdates
    {
        get => Settings.AutomaticallyCheckForUpdates;
        set => Settings.AutomaticallyCheckForUpdates = value;
    }

    string DownloadsFolderPath
    {
        get => Settings.DownloadsFolderPath;
        set => Settings.DownloadsFolderPath = value;
    }

    bool GenerateGlobalManifestPackage
    {
        get => Settings.GenerateGlobalManifestPackage;
        set => Settings.GenerateGlobalManifestPackage = value;
    }

    string InstallationFolderPath
    {
        get => Settings.InstallationFolderPath;
        set => Settings.InstallationFolderPath = value;
    }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

    bool ScanForCacheStaleness
    {
        get => Settings.ScanForCacheStaleness;
        set => Settings.ScanForCacheStaleness = value;
    }

    bool ScanForCorruptMods
    {
        get => Settings.ScanForCorruptMods;
        set => Settings.ScanForCorruptMods = value;
    }

    bool ScanForCorruptScriptMods
    {
        get => Settings.ScanForCorruptScriptMods;
        set => Settings.ScanForCorruptScriptMods = value;
    }

    bool ScanForErrorLogs
    {
        get => Settings.ScanForErrorLogs;
        set => Settings.ScanForErrorLogs = value;
    }

    bool ScanForInvalidModSubdirectoryDepth
    {
        get => Settings.ScanForInvalidModSubdirectoryDepth;
        set => Settings.ScanForInvalidModSubdirectoryDepth = value;
    }

    bool ScanForInvalidScriptModSubdirectoryDepth
    {
        get => Settings.ScanForInvalidScriptModSubdirectoryDepth;
        set => Settings.ScanForInvalidScriptModSubdirectoryDepth = value;
    }

    bool ScanForLoose7ZipArchives
    {
        get => Settings.ScanForLoose7ZipArchives;
        set => Settings.ScanForLoose7ZipArchives = value;
    }

    bool ScanForLooseRarArchives
    {
        get => Settings.ScanForLooseRarArchives;
        set => Settings.ScanForLooseRarArchives = value;
    }

    bool ScanForLooseZipArchives
    {
        get => Settings.ScanForLooseZipArchives;
        set => Settings.ScanForLooseZipArchives = value;
    }

    bool ScanForMissingBe
    {
        get => Settings.ScanForMissingBe;
        set => Settings.ScanForMissingBe = value;
    }

    bool ScanForMissingDependency
    {
        get => Settings.ScanForMissingDependency;
        set => Settings.ScanForMissingDependency = value;
    }

    bool ScanForMissingMccc
    {
        get => Settings.ScanForMissingMccc;
        set => Settings.ScanForMissingMccc = value;
    }

    bool ScanForMissingModGuard
    {
        get => Settings.ScanForMissingModGuard;
        set => Settings.ScanForMissingModGuard = value;
    }

    bool ScanForMultipleModVersions
    {
        get => Settings.ScanForMultipleModVersions;
        set => Settings.ScanForMultipleModVersions = value;
    }

    bool ScanForMutuallyExclusiveMods
    {
        get => Settings.ScanForMutuallyExclusiveMods;
        set => Settings.ScanForMutuallyExclusiveMods = value;
    }

    bool ScanForModsDisabled
    {
        get => Settings.ScanForModsDisabled;
        set => Settings.ScanForModsDisabled = value;
    }

    bool ScanForScriptModsDisabled
    {
        get => Settings.ScanForScriptModsDisabled;
        set => Settings.ScanForScriptModsDisabled = value;
    }

    bool ScanForShowModsListAtStartupEnabled
    {
        get => Settings.ScanForShowModsListAtStartupEnabled;
        set => Settings.ScanForShowModsListAtStartupEnabled = value;
    }

    UserType Type
    {
        get => Settings.Type;
        set
        {
            Settings.Type = value;
            SetDefaultScansForUserType(value);
        }
    }

    string UserDataFolderPath
    {
        get => Settings.UserDataFolderPath;
        set => Settings.UserDataFolderPath = value;
    }

    /// <inheritdoc />
    public void Dispose() =>
        Settings.PropertyChanged -= HandleUserPreferencesChanged;

    async Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (targetIndex is >= 6)
        {
            if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            {
                if (!await DialogService.ShowCautionDialogAsync
                (
                    StringLocalizer[nameof(AppText.OnboardingDialog_Caution_SpookingMacs_Caption)],
                    StringLocalizer[nameof(AppText.OnboardingDialog_Caution_SpookingMacs_Text)]
                ))
                    return true;
            }
            if (defaultCreatorsChipSetField is not null)
                await defaultCreatorsChipSetField.CommitPendingEntryIfEmptyAsync();
            Settings.DefaultCreatorsList = string.Join(Environment.NewLine, defaultCreators);
            Settings.Onboarded = true;
            MudDialog?.Close(DialogResult.Ok(true));
            return false;
        }
        if (direction is StepChangeDirection.Backward)
            return false;
        switch (targetIndex)
        {
            case 3:
                await ScanForFoldersAsync();
                return false;
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
        StaticDispatcher.Dispatch(StateHasChanged);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.PropertyChanged += HandleUserPreferencesChanged;
        SetDefaultScansForUserType(Settings.Type);
    }

    async Task ScanForFoldersAsync()
    {
        if (foldersSelector is null)
            return;
        loadingText = StringLocalizer[nameof(AppText.OnboardingDialog_Folders_Loading)];
        isLoading = true;
        StateHasChanged();
        await foldersSelector.ScanForFoldersAsync();
        isLoading = false;
        StateHasChanged();
        return;
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    void SetDefaultScansForUserType(UserType value)
    {
        switch (value)
        {
            case UserType.Creator:
                Settings.ScanForModsDisabled = false;
                Settings.ScanForScriptModsDisabled = false;
                Settings.ScanForInvalidModSubdirectoryDepth = false;
                Settings.ScanForInvalidScriptModSubdirectoryDepth = false;
                Settings.ScanForLooseZipArchives = false;
                Settings.ScanForLooseRarArchives = false;
                Settings.ScanForLoose7ZipArchives = false;
                Settings.ScanForCorruptMods = false;
                Settings.ScanForCorruptScriptMods = false;
                Settings.ScanForErrorLogs = false;
                Settings.ScanForMissingMccc = false;
                Settings.ScanForMissingBe = false;
                Settings.ScanForMissingModGuard = false;
                Settings.ScanForMissingDependency = false;
                Settings.ScanForCacheStaleness = false;
                Settings.ScanForMultipleModVersions = false;
                Settings.ScanForMutuallyExclusiveMods = false;
                Settings.ScanForShowModsListAtStartupEnabled = false;
                break;
            default:
                Settings.ScanForModsDisabled = ScanAttribute.Get(typeof(IModSettingScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForScriptModsDisabled = ScanAttribute.Get(typeof(IScriptModSettingScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForInvalidModSubdirectoryDepth = ScanAttribute.Get(typeof(IPackageDepthScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForInvalidScriptModSubdirectoryDepth = ScanAttribute.Get(typeof(ITs4ScriptDepthScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForLooseZipArchives = ScanAttribute.Get(typeof(ILooseZipArchiveScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForLooseRarArchives = ScanAttribute.Get(typeof(ILooseRarArchiveScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForLoose7ZipArchives = ScanAttribute.Get(typeof(ILoose7ZipArchiveScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForCorruptMods = ScanAttribute.Get(typeof(IPackageCorruptScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForCorruptScriptMods = ScanAttribute.Get(typeof(ITs4ScriptCorruptScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForErrorLogs = ScanAttribute.Get(typeof(IErrorLogScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMissingMccc = ScanAttribute.Get(typeof(IMcccMissingScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMissingBe = ScanAttribute.Get(typeof(IBeMissingScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMissingModGuard = ScanAttribute.Get(typeof(IModGuardMissingScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMissingDependency = ScanAttribute.Get(typeof(IDependencyScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForCacheStaleness = ScanAttribute.Get(typeof(ICacheStalenessScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMultipleModVersions = ScanAttribute.Get(typeof(IMultipleModVersionsScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForMutuallyExclusiveMods = ScanAttribute.Get(typeof(IExclusivityScan))?.IsEnabledByDefault ?? false;
                Settings.ScanForShowModsListAtStartupEnabled = ScanAttribute.Get(typeof(IShowModListStartupSettingScan))?.IsEnabledByDefault ?? false;
                break;
        }
    }
}
