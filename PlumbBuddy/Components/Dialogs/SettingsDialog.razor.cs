using PlumbBuddy.Services;

namespace PlumbBuddy.Components.Dialogs;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
partial class SettingsDialog
{
    IReadOnlyList<string> defaultCreators = [];
    ChipSetField? defaultCreatorsChipSetField;
    FoldersSelector? foldersSelector;
    ModHoundExcludePackagesMode modHoundExcludePackagesMode;
    IReadOnlyList<string> modHoundPackagesExclusions = [];
    ChipSetField? modHoundPackagesExclusionsChipSetField;
    ModsDirectoryCatalogerState modsDirectoryCatalogerState;
    decimal originalUiZoom;
    MudTabs? tabs;
    ThemeSelector? themeSelector;
    UserType type;

    bool AllowModsToInterceptKeyStrokes { get; set; }

    [Parameter]
    public int ActivePanelIndex { get; set; }

    string ArchiveFolderPath { get; set; } = string.Empty;

    bool AutomaticallyCheckForUpdates { get; set; }

    string DownloadsFolderPath { get; set; } = string.Empty;

    bool ForceGameProcessPerformanceProcessorAffinity { get; set; }

    bool GenerateGlobalManifestPackage { get; set; }

    string InstallationFolderPath { get; set; } = string.Empty;

    ModHoundExcludePackagesMode ModHoundExcludePackagesMode
    {
        get => modHoundExcludePackagesMode;
        set
        {
            modHoundExcludePackagesMode = value;
            SampleModHoundPackagesBatchYield();
        }
    }

    int? ModHoundPackagesBatchYieldExcluded { get; set; }

    int? ModHoundPackagesBatchYieldIncluded { get; set; }

    IReadOnlyList<string> ModHoundPackagesExclusions
    {
        get => modHoundPackagesExclusions;
        set
        {
            modHoundPackagesExclusions = value;
            SampleModHoundPackagesBatchYield();
        }
    }

    long? ModHoundReportRetentionPeriodTicks { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    bool NotifyOnModKeyStrokeInterceptionChanges { get; set; }

    bool OfferPatchDayModUpdatesHelp { get; set; }

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

    bool ScanForWrongGameVersion { get; set; }

    bool ScanForWrongGameVersionSC { get; set; }

    bool ScanForWrongGameVersionTS2 { get; set; }

    bool ScanForWrongGameVersionTS3 { get; set; }

    bool ShowDlcRetailUsd { get; set; }

    bool ShowSystemTrayIcon { get; set; }

    [Inject]
    public ISuperSnacks SuperSnacks { get; set; } = null!;

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

    decimal UiZoom
    {
        get => Settings.UiZoom;
        set => Settings.UiZoom = value;
    }

    int UiZoomPercent
    {
        get => (int)(UiZoom * 100M);
        set => UiZoom = value / 100M;
    }

    string UserDataFolderPath { get; set; } = string.Empty;

    public void Dispose() =>
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
        {
            StaticDispatcher.Dispatch(() =>
            {
                modsDirectoryCatalogerState = ModsDirectoryCataloger.State;
                if (modsDirectoryCatalogerState is not ModsDirectoryCatalogerState.Idle)
                    GenerateGlobalManifestPackage = Settings.GenerateGlobalManifestPackage;
            });
            if (ModsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
                SampleModHoundPackagesBatchYield();
        }
    }

    void HandleSetModHoundReportRetentionPeriodDefault() =>
        ModHoundReportRetentionPeriodTicks = 4L * 7 * 24 * 60 * 60 * 10_000_000;

    void HandleSetModHoundReportRetentionPeriodIndefinite() =>
        ModHoundReportRetentionPeriodTicks = null;

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.UiZoom))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSetUiZoomDefault() =>
        UiZoom = 1;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            defaultCreators = [..Settings.DefaultCreatorsList.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
            modHoundPackagesExclusions = Settings.ModHoundPackagesExclusions;
            SampleModHoundPackagesBatchYield();
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

    protected override void OnInitialized()
    {
        modsDirectoryCatalogerState = ModsDirectoryCataloger.State;
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        originalUiZoom = Settings.UiZoom;
        AutomaticallyCheckForUpdates = Settings.AutomaticallyCheckForUpdates;
        ForceGameProcessPerformanceProcessorAffinity = Settings.ForceGameProcessPerformanceProcessorAffinity;
        GenerateGlobalManifestPackage = Settings.GenerateGlobalManifestPackage;
        AllowModsToInterceptKeyStrokes = Settings.AllowModsToInterceptKeyStrokes;
        NotifyOnModKeyStrokeInterceptionChanges = Settings.NotifyOnModKeyStrokeInterceptionChanges;
        modHoundExcludePackagesMode = Settings.ModHoundExcludePackagesMode;
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
        ScanForWrongGameVersion = Settings.ScanForWrongGameVersion;
        ScanForWrongGameVersionSC = Settings.ScanForWrongGameVersionSC;
        ScanForWrongGameVersionTS2 = Settings.ScanForWrongGameVersionTS2;
        ScanForWrongGameVersionTS3 = Settings.ScanForWrongGameVersionTS3;
        ShowDlcRetailUsd = Settings.ShowDlcRetailUsd;
        ShowSystemTrayIcon = Settings.ShowSystemTrayIcon;
        Type = Settings.Type;
    }

    void CancelOnClickHandler()
    {
        UiZoom = originalUiZoom;
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
        if (modHoundPackagesExclusionsChipSetField is not null)
            await modHoundPackagesExclusionsChipSetField.CommitPendingEntryIfEmptyAsync();
        Settings.ArchiveFolderPath = ArchiveFolderPath;
        Settings.AutomaticallyCheckForUpdates = AutomaticallyCheckForUpdates;
        Settings.DefaultCreatorsList = string.Join(Environment.NewLine, defaultCreators);
        Settings.DownloadsFolderPath = DownloadsFolderPath;
        Settings.ForceGameProcessPerformanceProcessorAffinity = ForceGameProcessPerformanceProcessorAffinity;
        Settings.GenerateGlobalManifestPackage = GenerateGlobalManifestPackage;
        Settings.AllowModsToInterceptKeyStrokes = AllowModsToInterceptKeyStrokes;
        Settings.NotifyOnModKeyStrokeInterceptionChanges = NotifyOnModKeyStrokeInterceptionChanges;
        Settings.InstallationFolderPath = InstallationFolderPath;
        Settings.ModHoundExcludePackagesMode = ModHoundExcludePackagesMode;
        Settings.ModHoundPackagesExclusions = ModHoundPackagesExclusions.ToImmutableArray();
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
        Settings.ScanForWrongGameVersion = ScanForWrongGameVersion;
        Settings.ScanForWrongGameVersionSC = ScanForWrongGameVersionSC;
        Settings.ScanForWrongGameVersionTS2 = ScanForWrongGameVersionTS2;
        Settings.ScanForWrongGameVersionTS3 = ScanForWrongGameVersionTS3;
        Settings.ShowDlcRetailUsd = ShowDlcRetailUsd;
        Settings.ShowSystemTrayIcon = ShowSystemTrayIcon;
        Settings.Type = Type;
        Settings.UserDataFolderPath = UserDataFolderPath;
        MudDialog?.Close(DialogResult.Ok(true));
    }

    void SampleModHoundPackagesBatchYield() =>
        _ = Task.Run(SampleModHoundPackagesBatchYieldAsync);

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task SampleModHoundPackagesBatchYieldAsync()
    {
        try
        {
            using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var files = (await pbDbContext.ModFiles // get me dem mod files MDC
                .Where
                (
                    mf =>
                    mf.FoundAbsent == null
                    &&
                    (
                        mf.FileType == ModsDirectoryFileType.Package // if it's a package
                        && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length <= 5 // and 5 folders deep or less
                        || mf.FileType == ModsDirectoryFileType.ScriptArchive // or it's a script mod
                        && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length <= 1
                    )// and 1 folder deep or less
                )
                .OrderBy(mf => mf.Path) // put them in order like a gentleman
                .Select(mf => mf.Path.Replace("\\", "/")) // MH thinks in POSIX
                .ToListAsync()
                .ConfigureAwait(false))
                .Select(p => new
                {
                    extension = p[(p.LastIndexOf('.') + 1)..],
                    fullPath = p
                })
                .ToImmutableArray();
            var exclusionTests = modHoundExcludePackagesMode switch
            {
                ModHoundExcludePackagesMode.Patterns => modHoundPackagesExclusions.Select(exclusion =>
                {
                    try
                    {
                        var pattern = new Regex(exclusion, RegexOptions.IgnoreCase);
                        return (Func<string, bool>)(path => pattern.IsMatch(path));
                    }
                    catch (RegexParseException)
                    {
                        SuperSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModHoundClient_Snack_Error_InvalidPackageExclusionRegex, exclusion)), Severity.Error, options =>
                        {
                            options.Icon = MaterialDesignIcons.Normal.Regex;
                            options.Action = AppText.ModHoundClient_SnackAction_LoadSandbox;
                            options.OnClick = async _ =>
                            {
                                if (await DialogService.ShowQuestionDialogAsync(AppText.ModHoundClient_Question_PackageFilesInClipboard_Caption, AppText.ModHoundClient_Question_PackageFilesInClipboard_Text) ?? false)
                                    await Clipboard.SetTextAsync(string.Join(Environment.NewLine, files.Select(file => file.fullPath)));
                                await Browser.OpenAsync($"https://regex101.com/?regex={Uri.EscapeDataString(exclusion)}&flags=gim&flavor=dotnet", BrowserLaunchMode.External);
                            };
                            options.RequireInteraction = true;
                        });
                        throw;
                    }
                }),
                _ => modHoundPackagesExclusions.Select(exclusion => (Func<string, bool>)(path => path.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase)))
            };
            var packages = files.Where(file => file.extension.Equals("package", StringComparison.OrdinalIgnoreCase));
            var batchYield = packages.ToLookup(file => !exclusionTests.Any(exclusionTest => exclusionTest(file.fullPath)));
            ModHoundPackagesBatchYieldIncluded = batchYield[true].Count();
            ModHoundPackagesBatchYieldExcluded = batchYield[false].Count();
        }
        catch
        {
            ModHoundPackagesBatchYieldIncluded = null;
            ModHoundPackagesBatchYieldExcluded = null;
        }
        finally
        {
            StaticDispatcher.Dispatch(StateHasChanged);
        }
    }
}
