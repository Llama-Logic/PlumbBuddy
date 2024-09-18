namespace PlumbBuddy.Components.Controls;

partial class ScansToggler
{
    public ScansToggler()
    {
        gameOptionScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForModsDisabled), "...Mods are disabled even though you have .package files" },
            { nameof(ScanForScriptModsDisabled), "...Script Mods are disabled even though you have .ts4script files" }
        }.ToImmutableDictionary();
        gameOptionsScanLabels =
        [
            gameOptionScanLabelByName[nameof(ScanForModsDisabled)],
            gameOptionScanLabelByName[nameof(ScanForScriptModsDisabled)]
        ];
        foundScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForInvalidModSubdirectoryDepth), "...a .package file more than five subfolders deep" },
            { nameof(ScanForInvalidScriptModSubdirectoryDepth), "...a .ts4script file more than one subfolder deep" },
            { nameof(ScanForLooseZipArchives), "...a .zip file" },
            { nameof(ScanForLooseRarArchives), "...a .rar file" },
            { nameof(ScanForLoose7ZipArchives), "...a .7z file" },
            { nameof(ScanForErrorLogs), "...a file that appears to contain error information" }
        }.ToImmutableDictionary();
        foundScanLabels =
        [
            foundScanLabelByName[nameof(ScanForInvalidModSubdirectoryDepth)],
            foundScanLabelByName[nameof(ScanForInvalidScriptModSubdirectoryDepth)],
            foundScanLabelByName[nameof(ScanForLooseZipArchives)],
            foundScanLabelByName[nameof(ScanForLooseRarArchives)],
            foundScanLabelByName[nameof(ScanForLoose7ZipArchives)],
            foundScanLabelByName[nameof(ScanForErrorLogs)]
        ];
        notFoundScanLabelByName = new Dictionary<string, string>
        {
            { nameof(ScanForMissingMccc), "...Deaderpool's MC Command Center" },
            { nameof(ScanForMissingBe), "...TwistedMexi's Better Exceptions" },
            { nameof(ScanForMissingModGuard), "...TwistedMexi's Mod Guard" },
            { nameof(ScanForMissingDependency), "...a pack or mod required by another mod you have installed" }
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
            { nameof(ScanForCacheStaleness), "...it is appropriate to clear the cache files" },
            { nameof(ScanForResourceConflicts), "...a resource override conflict is occurring" },
            { nameof(ScanForMultipleModVersions), "...you're loading multiple versions of the same mod" }
        }.ToImmutableDictionary();
        analysisScanLabels =
        [
            analysisScanLabelByName[nameof(ScanForCacheStaleness)],
            analysisScanLabelByName[nameof(ScanForResourceConflicts)],
            analysisScanLabelByName[nameof(ScanForMultipleModVersions)]
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
    public bool ScanForResourceConflicts { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForResourceConflictsChanged { get; set; }

    [Parameter]
    public bool ScanForScriptModsDisabled { get; set; }

    [Parameter]
    public EventCallback<bool> ScanForScriptModsDisabledChanged { get; set; }

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
        ScanForErrorLogs = false;
        ScanForInvalidModSubdirectoryDepth = false;
        ScanForInvalidScriptModSubdirectoryDepth = false;
        ScanForLoose7ZipArchives = false;
        ScanForLooseRarArchives = false;
        ScanForLooseZipArchives = false;
        ScanForMissingBe = false;
        ScanForMissingDependency = false;
        ScanForMissingMccc = false;
        ScanForMissingModGuard = false;
        ScanForModsDisabled = false;
        ScanForMultipleModVersions = false;
        ScanForResourceConflicts = false;
        ScanForScriptModsDisabled = false;
        StateHasChanged();
    }

    void EnableAllOnClickHandler()
    {
        ScanForCacheStaleness = true;
        ScanForErrorLogs = true;
        ScanForInvalidModSubdirectoryDepth = true;
        ScanForInvalidScriptModSubdirectoryDepth = true;
        ScanForLoose7ZipArchives = true;
        ScanForLooseRarArchives = true;
        ScanForLooseZipArchives = true;
        ScanForMissingBe = true;
        ScanForMissingDependency = true;
        ScanForMissingMccc = true;
        ScanForMissingModGuard = true;
        ScanForModsDisabled = true;
        ScanForMultipleModVersions = true;
        ScanForResourceConflicts = true;
        ScanForScriptModsDisabled = true;
        StateHasChanged();
    }

    IEnumerable<string> GetSelectedScans(IReadOnlyDictionary<string, string> scanLabelByName)
    {
        var type = GetType();
        foreach (var (name, label) in scanLabelByName)
            if ((bool)type.GetProperty(name)!.GetValue(this)!)
                yield return label;
    }

    void SetDefaultOnClickHandler()
    {
        ScanForCacheStaleness = ScanAttribute.Get(typeof(ICacheStalenessScan))?.IsEnabledByDefault ?? false;
        ScanForErrorLogs = ScanAttribute.Get(typeof(IErrorLogScan))?.IsEnabledByDefault ?? false;
        ScanForInvalidModSubdirectoryDepth = ScanAttribute.Get(typeof(IPackageDepthScan))?.IsEnabledByDefault ?? false;
        ScanForInvalidScriptModSubdirectoryDepth = ScanAttribute.Get(typeof(ITs4ScriptDepthScan))?.IsEnabledByDefault ?? false;
        ScanForLoose7ZipArchives = ScanAttribute.Get(typeof(ILoose7ZipArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForLooseRarArchives = ScanAttribute.Get(typeof(ILooseRarArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForLooseZipArchives = ScanAttribute.Get(typeof(ILooseZipArchiveScan))?.IsEnabledByDefault ?? false;
        ScanForMissingBe = ScanAttribute.Get(typeof(IBeMissingScan))?.IsEnabledByDefault ?? false;
        ScanForMissingDependency = ScanAttribute.Get(typeof(IDependencyMissingScan))?.IsEnabledByDefault ?? false;
        ScanForMissingMccc = ScanAttribute.Get(typeof(IMcccMissingScan))?.IsEnabledByDefault ?? false;
        ScanForMissingModGuard = ScanAttribute.Get(typeof(IModGuardMissingScan))?.IsEnabledByDefault ?? false;
        ScanForModsDisabled = ScanAttribute.Get(typeof(IModSettingScan))?.IsEnabledByDefault ?? false;
        ScanForMultipleModVersions = ScanAttribute.Get(typeof(IMultipleModVersionsScan))?.IsEnabledByDefault ?? false;
        ScanForResourceConflicts = ScanAttribute.Get(typeof(IResourceConflictScan))?.IsEnabledByDefault ?? false;
        ScanForScriptModsDisabled = ScanAttribute.Get(typeof(IScriptModSettingScan))?.IsEnabledByDefault ?? false;
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
