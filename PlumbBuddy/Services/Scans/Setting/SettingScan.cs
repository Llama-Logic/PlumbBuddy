namespace PlumbBuddy.Services.Scans.Setting;

public abstract class SettingScan :
    Scan,
    ISettingScan
{
    protected SettingScan(PbDbContext pbDbContext, IPlayer player, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, ModsDirectoryFileType modDirectoryFileType, string deadScanIssueData, string deadScanIssueFixResolutionData, string deadScanIssueStopResolutionData)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadScanIssueData);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadScanIssueFixResolutionData);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadScanIssueStopResolutionData);
        this.pbDbContext = pbDbContext;
        this.player = player;
        this.smartSimObserver = smartSimObserver;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.modDirectoryFileType = modDirectoryFileType;
        this.deadScanIssueData = deadScanIssueData;
        this.deadScanIssueFixResolutionData = deadScanIssueFixResolutionData;
        this.deadScanIssueStopResolutionData = deadScanIssueStopResolutionData;
    }

    readonly string deadScanIssueData;
    readonly string deadScanIssueFixResolutionData;
    readonly string deadScanIssueStopResolutionData;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly ModsDirectoryFileType modDirectoryFileType;
    readonly PbDbContext pbDbContext;
    readonly IPlayer player;
    readonly ISmartSimObserver smartSimObserver;
    readonly ISuperSnacks superSnacks;

    protected abstract bool AreGameOptionsDisablingFeature(ISmartSimObserver smartSimObserver);

    protected abstract void CorrectIniOptions(IniParser.Model.KeyDataCollection options);

    protected abstract ScanIssue GenerateDeadScanIssue();

    protected abstract ScanIssue GenerateHealthyScanIssue();

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string issueDataStr && issueDataStr == deadScanIssueData && resolutionData is string resolutionDataStr)
        {
            if (resolutionDataStr == deadScanIssueFixResolutionData)
            {
                var optionsIniFile = new FileInfo(Path.Combine(player.UserDataFolderPath, "Options.ini"));
                if (!optionsIniFile.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because your Game Options file is missing. You need to launch the game, close it, and check back here."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileAlert);
                    return;
                }
                if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Sleeping)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the game is currently running. You need to close it and check back here."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.LockAlert);
                    return;
                }
                var parser = new IniDataParser();
                var data = parser.Parse(await File.ReadAllTextAsync(optionsIniFile.FullName).ConfigureAwait(false));
                CorrectIniOptions(data["options"]);
                await File.WriteAllTextAsync(optionsIniFile.FullName, data.ToString()).ConfigureAwait(false);
                superSnacks.OfferRefreshments(new MarkupString("I have changed your Game Options for you at your request."), Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.AutoFix);
                return;
            }
            if (resolutionDataStr == deadScanIssueStopResolutionData)
            {
                StopScanning(player);
                return;
            }
        }
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        if (await pbDbContext.ModFiles.AnyAsync(mf => mf.Path != null && mf.FileType == modDirectoryFileType).ConfigureAwait(false)
            && AreGameOptionsDisablingFeature(smartSimObserver))
            yield return GenerateDeadScanIssue();
        else
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(IPlayer player);
}
