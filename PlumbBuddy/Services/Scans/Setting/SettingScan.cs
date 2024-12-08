namespace PlumbBuddy.Services.Scans.Setting;

public abstract class SettingScan :
    Scan,
    ISettingScan
{
    protected SettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, ModsDirectoryFileType modDirectoryFileType, string undesirableScanIssueData, string undesirableScanIssueFixResolutionData, string undesirableScanIssueStopResolutionData)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentException.ThrowIfNullOrWhiteSpace(undesirableScanIssueData);
        ArgumentException.ThrowIfNullOrWhiteSpace(undesirableScanIssueFixResolutionData);
        ArgumentException.ThrowIfNullOrWhiteSpace(undesirableScanIssueStopResolutionData);
        this.pbDbContextFactory = pbDbContextFactory;
        this.settings = settings;
        this.smartSimObserver = smartSimObserver;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.modDirectoryFileType = modDirectoryFileType;
        this.undesirableScanIssueData = undesirableScanIssueData;
        this.undesirableScanIssueFixResolutionData = undesirableScanIssueFixResolutionData;
        this.undesirableScanIssueStopResolutionData = undesirableScanIssueStopResolutionData;
    }

    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly ModsDirectoryFileType modDirectoryFileType;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly ISettings settings;
    readonly ISmartSimObserver smartSimObserver;
    readonly ISuperSnacks superSnacks;
    readonly string undesirableScanIssueData;
    readonly string undesirableScanIssueFixResolutionData;
    readonly string undesirableScanIssueStopResolutionData;

    protected abstract bool AreGameOptionsUndesirable(ISmartSimObserver smartSimObserver);

    protected abstract void CorrectIniOptions(IniParser.Model.KeyDataCollection options);

    protected abstract ScanIssue GenerateUndesirableScanIssue();

    protected abstract ScanIssue GenerateHealthyScanIssue();

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && issueDataStr == undesirableScanIssueData && resolutionData is string resolutionDataStr)
        {
            if (resolutionDataStr == undesirableScanIssueFixResolutionData)
            {
                var optionsIniFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Options.ini"));
                if (!optionsIniFile.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Setting_Fix_Error_OptionsMissing), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileAlert);
                    return;
                }
                if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Sleeping)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Setting_Fix_Error_GameIsRunning), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.LockAlert);
                    return;
                }
                var parser = new IniDataParser();
                var data = parser.Parse(await File.ReadAllTextAsync(optionsIniFile.FullName).ConfigureAwait(false));
                CorrectIniOptions(data["options"]);
                await File.WriteAllTextAsync(optionsIniFile.FullName, data.ToString()).ConfigureAwait(false);
                superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Setting_Fix_Success), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.AutoFix);
                return;
            }
            if (resolutionDataStr == undesirableScanIssueStopResolutionData)
            {
                StopScanning(settings);
                return;
            }
        }
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await pbDbContext.ModFiles.AnyAsync(mf => mf.Path != null && mf.FileType == modDirectoryFileType).ConfigureAwait(false)
            && AreGameOptionsUndesirable(smartSimObserver))
            yield return GenerateUndesirableScanIssue();
        else
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(ISettings settings);
}
