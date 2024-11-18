namespace PlumbBuddy.Services.Scans.Corrupt;

public abstract class CorruptScan :
    Scan,
    ICorruptScan
{
    protected CorruptScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks, ModsDirectoryFileType modsDirectoryFileType, int maximumDepth)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.superSnacks = superSnacks;
        this.modsDirectoryFileType = modsDirectoryFileType;
        this.maximumDepth = maximumDepth;
    }

    readonly int maximumDepth;
    readonly ModsDirectoryFileType modsDirectoryFileType;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    protected abstract ScanIssue GenerateHealthyScanIssue();

    protected abstract ScanIssue GenerateDeadScanIssue(FileInfo file, ModFile modFile);

    protected abstract ScanIssue GenerateUncomfortableScanIssue(FileInfo file, ModFile modFile);

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string looseArchiveRelativePath && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "moveToDownloads")
            {
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", looseArchiveRelativePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the file done wandered off."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileQuestion);
                    return Task.CompletedTask;
                }
                var downloads = settings.DownloadsFolderPath;
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
                StopScanning(settings);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        var anyCheeseSlidOffTheCracker = false;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var offendingModFile in pbDbContext.ModFiles.Where(mf => mf.Path != null && mf.FileType == modsDirectoryFileType).AsAsyncEnumerable())
        {
            anyCheeseSlidOffTheCracker = true;
            var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", offendingModFile.Path!));
            if (offendingModFile.Path!.AsSpan().Count(Path.DirectorySeparatorChar) > maximumDepth)
                yield return GenerateUncomfortableScanIssue(file, offendingModFile);
            else
                yield return GenerateDeadScanIssue(file, offendingModFile);
        }
        if (!anyCheeseSlidOffTheCracker)
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(ISettings settings);
}
