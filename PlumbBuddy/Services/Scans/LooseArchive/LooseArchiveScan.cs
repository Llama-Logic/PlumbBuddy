namespace PlumbBuddy.Services.Scans.LooseArchive;

public abstract class LooseArchiveScan :
    Scan,
    ILooseArchiveScan
{
    protected LooseArchiveScan(IPlatformFunctions platformFunctions, PbDbContext pbDbContext, IPlayer player, ISuperSnacks superSnacks, ModsDirectoryFileType modDirectoryFileType)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.platformFunctions = platformFunctions;
        this.pbDbContext = pbDbContext;
        this.player = player;
        this.superSnacks = superSnacks;
        this.modDirectoryFileType = modDirectoryFileType;
    }

    readonly ModsDirectoryFileType modDirectoryFileType;
    readonly PbDbContext pbDbContext;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    readonly ISuperSnacks superSnacks;

    protected abstract ScanIssue GenerateHealthyScanIssue();

    protected abstract ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest);

    public override Task ResolveIssueAsync(ILifetimeScope interfaceLifetimeScope, object issueData, object resolutionData)
    {
        if (issueData is string looseArchiveRelativePath && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "moveToDownloads")
            {
                var file = new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", looseArchiveRelativePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the file done wandered off."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileQuestion);
                    return Task.CompletedTask;
                }
                var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var prospectiveTargetPath = Path.Combine(downloads, file.Name);
                var dupeCount = 1;
                while (File.Exists(prospectiveTargetPath))
                    prospectiveTargetPath = Path.Combine(downloads, $"{file.Name[..^file.Extension.Length]} {++dupeCount}{file.Extension}");
                Exception? moveEx = null;
                try
                {
                    file.MoveTo(prospectiveTargetPath);
                }
                catch (Exception ex)
                {
                    moveEx = ex;
                }
                if (moveEx is not null)
                {
                    superSnacks.OfferRefreshments(new MarkupString(
                        $"""
                        Boy, did that *not* work. Your computer's operating system said:

                        `{moveEx.GetType().Name}: {moveEx.Message}`
                        """), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileAlert);
                    return Task.CompletedTask;
                }
                var newFile = new FileInfo(prospectiveTargetPath);
                superSnacks.OfferRefreshments(new MarkupString($"Okay, the file has been safely moved to its new home in your Downloads folder and is called <strong>{newFile.Name}</strong> there."), Severity.Success, options =>
                {
                    options.Icon = MaterialDesignIcons.Normal.FolderMove;
                    options.Action = "Show me";
                    options.VisibleStateDuration = 30000;
                    options.Onclick = _ =>
                    {
                        platformFunctions.ViewFile(newFile);
                        return Task.CompletedTask;
                    };
                });
                return Task.CompletedTask;
            }
            if (resolutionCmd is "stopTellingMe")
            {
                StopScanning(player);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        var foundNaughtyLooseArchives = false;
        await foreach (var naughtyLooseArchive in pbDbContext.FilesOfInterest.Where(foi => foi.FileType == modDirectoryFileType).AsAsyncEnumerable())
        {
            foundNaughtyLooseArchives = true;
            yield return GenerateUncomfortableScanIssue(new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", naughtyLooseArchive.Path)), naughtyLooseArchive);
        }
        if (!foundNaughtyLooseArchives)
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(IPlayer player);
}
