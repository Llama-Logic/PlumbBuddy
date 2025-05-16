namespace PlumbBuddy.Services.Scans.LooseArchive;

public abstract class LooseArchiveScan :
    Scan,
    ILooseArchiveScan
{
    protected LooseArchiveScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks, ModsDirectoryFileType modDirectoryFileType)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.superSnacks = superSnacks;
        this.modDirectoryFileType = modDirectoryFileType;
    }

    readonly ModsDirectoryFileType modDirectoryFileType;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    protected abstract ScanIssue GenerateHealthyScanIssue();

    protected abstract ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest);

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string looseArchiveRelativePath && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "moveToDownloads")
            {
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, looseArchiveRelativePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_LooseArchive_MoveToDownloads_Error_CannotFind), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FileQuestion;
                        options.RequireInteraction = true;
                    });
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
                    superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Scan_Common_Error_CannotMove, moveEx.GetType().Name, moveEx.Message)), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FileAlert;
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                var newFile = new FileInfo(prospectiveTargetPath);
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Scan_LooseArchive_MoveToDownloads_Success, newFile.Name)), Severity.Success, options =>
                {
                    options.Icon = MaterialDesignIcons.Normal.FolderMove;
                    options.Action = AppText.Common_ShowMe;
                    options.VisibleStateDuration = 30000;
                    options.OnClick = _ =>
                    {
                        platformFunctions.ViewFile(newFile);
                        return Task.CompletedTask;
                    };
                });
                return Task.CompletedTask;
            }
            if (resolutionCmd is "show")
            {
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, looseArchiveRelativePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Corrupt_ShowFile_Error_NotFound), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FileQuestion;
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                platformFunctions.ViewFile(file);
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
        var prefix = $"Mods{Path.DirectorySeparatorChar}";
        var foundNaughtyLooseArchives = false;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var naughtyLooseArchive in pbDbContext.FilesOfInterest.Where(foi => foi.FileType == modDirectoryFileType && foi.Path.StartsWith(prefix)).AsAsyncEnumerable())
        {
            foundNaughtyLooseArchives = true;
            yield return GenerateUncomfortableScanIssue(new FileInfo(Path.Combine(settings.UserDataFolderPath, naughtyLooseArchive.Path)), naughtyLooseArchive);
        }
        if (!foundNaughtyLooseArchives)
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(ISettings settings);
}
