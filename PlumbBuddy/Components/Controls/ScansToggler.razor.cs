namespace PlumbBuddy.Components.Controls;

partial class ScansToggler
{
    public ScansToggler()
    {
        gameOptionScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForModsDisabled), AppText.ScansToggler_ScanForModsDisabled_Label },
            { nameof(ScanForScriptModsDisabled), AppText.ScansToggler_ScanForScriptModsDisabled_Label },
            { nameof(ScanForShowModsListAtStartupEnabled), AppText.ScansToggler_ScanForShowModsListAtStartupEnabled_Label }
        }.ToImmutableDictionary();
        gameOptionsScanLabels =
        [
            gameOptionScanLabelByName[nameof(ScanForModsDisabled)],
            gameOptionScanLabelByName[nameof(ScanForScriptModsDisabled)],
            gameOptionScanLabelByName[nameof(ScanForShowModsListAtStartupEnabled)]
        ];
        foundScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForInvalidModSubdirectoryDepth), AppText.ScansToggler_ScanForInvalidModSubdirectoryDepth_Label },
            { nameof(ScanForInvalidScriptModSubdirectoryDepth), AppText.ScansToggler_ScanForInvalidScriptModSubdirectoryDepth_Label },
            { nameof(ScanForLooseZipArchives), AppText.ScansToggler_ScanForLooseZipArchives_Label },
            { nameof(ScanForLooseRarArchives), AppText.ScansToggler_ScanForLooseRarArchives_Label },
            { nameof(ScanForLoose7ZipArchives), AppText.ScansToggler_ScanForLoose7ZipArchives_Label },
            { nameof(ScanForCorruptMods), AppText.ScansToggler_ScanForCorruptMods_Label },
            { nameof(ScanForCorruptScriptMods), AppText.ScansToggler_ScanForCorruptScriptMods_Label },
            { nameof(ScanForErrorLogs), AppText.ScansToggler_ScanForErrorLogs_Label }
        }.ToImmutableDictionary();
        foundScanLabels =
        [
            foundScanLabelByName[nameof(ScanForInvalidModSubdirectoryDepth)],
            foundScanLabelByName[nameof(ScanForInvalidScriptModSubdirectoryDepth)],
            foundScanLabelByName[nameof(ScanForLooseZipArchives)],
            foundScanLabelByName[nameof(ScanForLooseRarArchives)],
            foundScanLabelByName[nameof(ScanForLoose7ZipArchives)],
            foundScanLabelByName[nameof(ScanForCorruptMods)],
            foundScanLabelByName[nameof(ScanForCorruptScriptMods)],
            foundScanLabelByName[nameof(ScanForErrorLogs)]
        ];
        notFoundScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForMissingModGuard), AppText.ScansToggler_ScanForMissingModGuard_Label },
            { nameof(ScanForMissingDependency), AppText.ScansToggler_ScanForMissingDependency_Label },
            { nameof(ScanForMissingMccc), AppText.ScansToggler_ScanForMissingMccc_Label },
            { nameof(ScanForMissingBe), AppText.ScansToggler_ScanForMissingBe_Label }
        }.ToImmutableDictionary();
        notFoundScanLabels =
        [
            notFoundScanLabelByName[nameof(ScanForMissingMccc)],
            notFoundScanLabelByName[nameof(ScanForMissingBe)],
            notFoundScanLabelByName[nameof(ScanForMissingModGuard)],
            notFoundScanLabelByName[nameof(ScanForMissingDependency)]
        ];
        analysisScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForCacheStaleness), AppText.ScansToggler_ScanForCacheStaleness_Label },
            { nameof(ScanForMultipleModVersions), AppText.ScansToggler_ScanForMultipleModVersions_Label },
            { nameof(ScanForMutuallyExclusiveMods), AppText.ScansToggler_ScanForMutuallyExclusiveMods_Label },
            { nameof(ScanForMismatchedInscribedHashes), AppText.ScansToggler_ScanForMismatchedInscribedHashes_Label }
        }.ToImmutableDictionary();
        analysisScanLabels =
        [
            analysisScanLabelByName[nameof(ScanForCacheStaleness)],
            analysisScanLabelByName[nameof(ScanForMultipleModVersions)],
            analysisScanLabelByName[nameof(ScanForMutuallyExclusiveMods)],
            analysisScanLabelByName[nameof(ScanForMismatchedInscribedHashes)]
        ];
    }

    readonly IReadOnlyDictionary<string, string> analysisScanLabelByName;
    readonly IReadOnlyList<string> analysisScanLabels;
    readonly IReadOnlyDictionary<string, string> foundScanLabelByName;
    readonly IReadOnlyList<string> foundScanLabels;
    readonly IReadOnlyDictionary<string, string> gameOptionScanLabelByName;
    readonly IReadOnlyList<string> gameOptionsScanLabels;
    readonly IReadOnlyDictionary<string, string> notFoundScanLabelByName;
    readonly IReadOnlyList<string> notFoundScanLabels;

    [Parameter]
    public bool ScanForCacheStaleness { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForCacheStalenessChanged { get; set; }

    [Parameter]
    public bool ScanForCorruptMods { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForCorruptModsChanged { get; set; }

    [Parameter]
    public bool ScanForCorruptScriptMods { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForCorruptScriptModsChanged { get; set; }

    [Parameter]
    public bool ScanForErrorLogs { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForErrorLogsChanged { get; set; }

    [Parameter]
    public bool ScanForInvalidModSubdirectoryDepth { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForInvalidModSubdirectoryDepthChanged { get; set; }

    [Parameter]
    public bool ScanForInvalidScriptModSubdirectoryDepth { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForInvalidScriptModSubdirectoryDepthChanged { get; set; }

    [Parameter]
    public bool ScanForLoose7ZipArchives { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForLoose7ZipArchivesChanged { get; set; }

    [Parameter]
    public bool ScanForLooseRarArchives { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForLooseRarArchivesChanged { get; set; }

    [Parameter]
    public bool ScanForLooseZipArchives { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForLooseZipArchivesChanged { get; set; }

    [Parameter]
    public bool ScanForMismatchedInscribedHashes { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMismatchedInscribedHashesChanged { get; set; }

    [Parameter]
    public bool ScanForMissingBe { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMissingBeChanged { get; set; }

    [Parameter]
    public bool ScanForMissingDependency { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMissingDependencyChanged { get; set; }

    [Parameter]
    public bool ScanForMissingMccc { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMissingMcccChanged { get; set; }

    [Parameter]
    public bool ScanForMissingModGuard { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMissingModGuardChanged { get; set; }

    [Parameter]
    public bool ScanForModsDisabled { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForModsDisabledChanged { get; set; }

    [Parameter]
    public bool ScanForMultipleModVersions { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMultipleModVersionsChanged { get; set; }

    [Parameter]
    public bool ScanForMutuallyExclusiveMods { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForMutuallyExclusiveModsChanged { get; set; }

    [Parameter]
    public bool ScanForScriptModsDisabled { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForScriptModsDisabledChanged { get; set; }

    [Parameter]
    public bool ScanForShowModsListAtStartupEnabled { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForShowModsListAtStartupEnabledChanged { get; set; }

    IEnumerable<string> SelectedAnalysisScans
    {
        get => GetSelectedScans(analysisScanLabelByName);
        set => SetSelectedScans(analysisScanLabelByName, value);
    }

    IEnumerable<string> SelectedFoundScans
    {
        get => GetSelectedScans(foundScanLabelByName);
        set => SetSelectedScans(foundScanLabelByName, value);
    }

    IEnumerable<string> SelectedGameOptionScans
    {
        get => GetSelectedScans(gameOptionScanLabelByName);
        set => SetSelectedScans(gameOptionScanLabelByName, value);
    }

    IEnumerable<string> SelectedNotFoundScans
    {
        get => GetSelectedScans(notFoundScanLabelByName);
        set => SetSelectedScans(notFoundScanLabelByName, value);
    }

    void DisableAllOnClickHandler()
    {
        ScanForCacheStaleness = false;
        ScanForCorruptMods = false;
        ScanForCorruptScriptMods = false;
        ScanForErrorLogs = false;
        ScanForInvalidModSubdirectoryDepth = false;
        ScanForInvalidScriptModSubdirectoryDepth = false;
        ScanForLoose7ZipArchives = false;
        ScanForLooseRarArchives = false;
        ScanForLooseZipArchives = false;
        ScanForMismatchedInscribedHashes = false;
        ScanForMissingBe = false;
        ScanForMissingDependency = false;
        ScanForMissingMccc = false;
        ScanForMissingModGuard = false;
        ScanForModsDisabled = false;
        ScanForMultipleModVersions = false;
        ScanForMutuallyExclusiveMods = false;
        ScanForScriptModsDisabled = false;
        ScanForShowModsListAtStartupEnabled = false;
        StateHasChanged();
    }

    void EnableAllOnClickHandler()
    {
        ScanForCacheStaleness = true;
        ScanForCorruptMods = true;
        ScanForCorruptScriptMods = true;
        ScanForErrorLogs = true;
        ScanForInvalidModSubdirectoryDepth = true;
        ScanForInvalidScriptModSubdirectoryDepth = true;
        ScanForLoose7ZipArchives = true;
        ScanForLooseRarArchives = true;
        ScanForLooseZipArchives = true;
        ScanForMismatchedInscribedHashes = true;
        ScanForMissingBe = true;
        ScanForMissingDependency = true;
        ScanForMissingMccc = true;
        ScanForMissingModGuard = true;
        ScanForModsDisabled = true;
        ScanForMultipleModVersions = true;
        ScanForMutuallyExclusiveMods = true;
        ScanForScriptModsDisabled = true;
        ScanForShowModsListAtStartupEnabled = true;
        StateHasChanged();
    }

    IEnumerable<string> GetSelectedScans(IReadOnlyDictionary<string, string> scanLabelByName)
    {
        var type = GetType();
        foreach (var (name, label) in scanLabelByName)
            if ((bool)type.GetProperty(name)!.GetValue(this)!)
                yield return label;
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    void SetDefaultOnClickHandler()
    {
        ScanForCacheStaleness = ScanAttribute.Get(typeof(ICacheStalenessScan))?.IsEnabledByDefault ?? false;
        ScanForCorruptMods = ScanAttribute.Get(typeof(IPackageCorruptScan))?.IsEnabledByDefault ?? false;
        ScanForCorruptScriptMods = ScanAttribute.Get(typeof(ITs4ScriptCorruptScan))?.IsEnabledByDefault ?? false;
        ScanForErrorLogs = ScanAttribute.Get(typeof(IErrorLogScan))?.IsEnabledByDefault ?? false;
        ScanForInvalidModSubdirectoryDepth = ScanAttribute.Get(typeof(IPackageDepthScan))?.IsEnabledByDefault ?? false;
        ScanForInvalidScriptModSubdirectoryDepth = ScanAttribute.Get(typeof(ITs4ScriptDepthScan))?.IsEnabledByDefault ?? false;
        ScanForLoose7ZipArchives = ScanAttribute.Get(typeof(ILoose7ZipArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForLooseRarArchives = ScanAttribute.Get(typeof(ILooseRarArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForLooseZipArchives = ScanAttribute.Get(typeof(ILooseZipArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForMismatchedInscribedHashes = ScanAttribute.Get(typeof(IMismatchedInscribedHashesScan))?.IsEnabledByDefault ?? false;
        ScanForMissingBe = ScanAttribute.Get(typeof(IBeMissingScan))?.IsEnabledByDefault ?? false;
        ScanForMissingDependency = ScanAttribute.Get(typeof(IDependencyScan))?.IsEnabledByDefault ?? false;
        ScanForMissingMccc = ScanAttribute.Get(typeof(IMcccMissingScan))?.IsEnabledByDefault ?? false;
        ScanForMissingModGuard = ScanAttribute.Get(typeof(IModGuardMissingScan))?.IsEnabledByDefault ?? false;
        ScanForModsDisabled = ScanAttribute.Get(typeof(IModSettingScan))?.IsEnabledByDefault ?? false;
        ScanForMultipleModVersions = ScanAttribute.Get(typeof(IMultipleModVersionsScan))?.IsEnabledByDefault ?? false;
        ScanForMutuallyExclusiveMods = ScanAttribute.Get(typeof(IExclusivityScan))?.IsEnabledByDefault ?? false;
        ScanForScriptModsDisabled = ScanAttribute.Get(typeof(IScriptModSettingScan))?.IsEnabledByDefault ?? false;
        ScanForShowModsListAtStartupEnabled = ScanAttribute.Get(typeof(IShowModListStartupSettingScan))?.IsEnabledByDefault ?? false;
        StateHasChanged();
    }

    void SetSelectedScans(IReadOnlyDictionary<string, string> scanLabelByName, IEnumerable<string> selectedScans)
    {
        var type = GetType();
        foreach (var (name, label) in scanLabelByName)
        {
            var value = selectedScans.Contains(label);
            type.GetProperty(name)!.SetValue(this, value);
            ((EventCallback<bool>)type.GetProperty($"{name}Changed")!.GetValue(this)!).InvokeAsync(value).Wait();
        }
    }
}
