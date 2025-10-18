namespace PlumbBuddy.Services.Scans.Depth;

public abstract class DepthScan :
    Scan,
    IDepthScan
{
    protected DepthScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, ModsDirectoryFileType modsDirectoryFileType, int maximumDepth)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.modsDirectoryFileType = modsDirectoryFileType;
        this.maximumDepth = maximumDepth;
    }

    readonly int maximumDepth;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly ModsDirectoryFileType modsDirectoryFileType;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    protected readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    protected abstract ScanIssue GenerateHealthyScanIssue();

    protected abstract ScanIssue GenerateSickScanIssue(FileInfo file, ModFile modFile);

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string modFilePath && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "move")
            {
                if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Sleeping)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Depth_MoveCloserToModsRoot_Error_GameIsRunning), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderLock;
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", modFilePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Depth_MoveCloserToModsRoot_Error_CannotFind), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FileQuestion;
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                if (file.Directory is not { } originDirectory)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Depth_MoveCloserToModsRoot_Error_CannotFindFolder), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderQuestion;
                        options.Action = AppText.Common_ShowMe;
                        options.OnClick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                var extentOfOffense = modFilePath.Count(c => c is '/' or '\\') - maximumDepth;
                var targetDirectory = originDirectory;
                while (--extentOfOffense >= 0 && targetDirectory?.Parent is { } nextTargetDirectory)
                    targetDirectory = nextTargetDirectory;
                if (targetDirectory is null)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Depth_MoveCloserToModsRoot_Error_WalkedBelowRoot), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderSwap;
                        options.Action = AppText.Common_ShowMe;
                        options.OnClick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                var originEntriesByConflicted = originDirectory.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly).ToLookup(originFileSystemEntry =>
                {
                    var targetPath = Path.Combine(targetDirectory.FullName, originFileSystemEntry.Name);
                    if (File.Exists(targetPath))
                        return !platformFunctions.DiscardableFileNamePatterns.Any(pattern => pattern.IsMatch(originFileSystemEntry.Name));
                    if (Directory.Exists(targetPath))
                        return !platformFunctions.DiscardableDirectoryNamePatterns.Any(pattern => pattern.IsMatch(originFileSystemEntry.Name));
                    return false;
                });
                var conflicts = originEntriesByConflicted[true].ToImmutableArray();
                if (conflicts.Any())
                {
                    superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Scan_Depth_MoveCloserToModsRoot_Error_OverwriteGuard, file.Name, targetDirectory.FullName, string.Join(Environment.NewLine, conflicts.Select(conflict => string.Format(AppText.Common_BulletListItem, conflict.Name))))), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderAlert;
                        options.Action = AppText.Common_ShowMe;
                        options.OnClick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                try
                {
                    foreach (var originEntry in originEntriesByConflicted[false])
                    {
                        if (originEntry is FileInfo originFile)
                            originFile.MoveTo(Path.Combine(targetDirectory.FullName, originFile.Name), true);
                        else if (originEntry is DirectoryInfo originSubDirectory)
                        {
                            var targetPath = Path.Combine(targetDirectory.FullName, originSubDirectory.Name);
                            if (Directory.Exists(targetPath))
                                Directory.Delete(targetPath, true);
                            originSubDirectory.MoveTo(targetPath);
                        }
                    }
                }
                catch (Exception moveEx)
                {
                    superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Scan_Common_Error_CannotMove, moveEx.GetType().Name, moveEx.Message)), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FileAlert;
                        options.RequireInteraction = true;
                    });
                    return Task.CompletedTask;
                }
                var newFile = new FileInfo(Path.Combine(targetDirectory.FullName, file.Name));
                superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Depth_MoveCloserToModsRoot_Success), Severity.Success, options =>
                {
                    options.Icon = MaterialDesignIcons.Normal.FolderMove;
                    options.Action = AppText.Common_ShowMe;
                    options.OnClick = _ =>
                    {
                        platformFunctions.ViewFile(newFile);
                        return Task.CompletedTask;
                    };
                    options.RequireInteraction = true;
                });
                return Task.CompletedTask;
            }
            if (resolutionCmd is "show")
            {
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", modFilePath));
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
        var anyLostInTheAbyss = false;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var offendingModFile in pbDbContext.ModFiles.Where(mf => mf.FoundAbsent == null && mf.FileType == modsDirectoryFileType && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length > maximumDepth).AsAsyncEnumerable())
        {
            anyLostInTheAbyss = true;
            yield return GenerateSickScanIssue(new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", offendingModFile.Path!)), offendingModFile);
        }
        if (!anyLostInTheAbyss)
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(ISettings settings);
}
