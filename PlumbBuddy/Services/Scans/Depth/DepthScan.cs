namespace PlumbBuddy.Services.Scans.Depth;

public abstract class DepthScan :
    Scan,
    IDepthScan
{
    protected DepthScan(IPlatformFunctions platformFunctions, PbDbContext pbDbContext, IPlayer player, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks, ModsDirectoryFileType modsDirectoryFileType, int maximumDepth)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.platformFunctions = platformFunctions;
        this.pbDbContext = pbDbContext;
        this.player = player;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        this.modsDirectoryFileType = modsDirectoryFileType;
        this.maximumDepth = maximumDepth;
    }

    readonly int maximumDepth;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly ModsDirectoryFileType modsDirectoryFileType;
    readonly PbDbContext pbDbContext;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
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
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the game is currently using the Mods folder. You'll need to close the game first."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FolderLock);
                    return Task.CompletedTask;
                }
                var file = new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", modFilePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the file done wandered off."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileQuestion);
                    return Task.CompletedTask;
                }
                if (file.Directory is not { } originDirectory)
                {
                    superSnacks.OfferRefreshments(new MarkupString("Hmm. I was unable to locate the folder this file is in... for some reason. Given that, I feel uncomfortable trying to move it."), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderQuestion;
                        options.Action = "Show me";
                        options.VisibleStateDuration = 30000;
                        options.Onclick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
                    });
                    return Task.CompletedTask;
                }
                var extentOfOffense = modFilePath.Count(c => c is '/' or '\\') - maximumDepth;
                var targetDirectory = originDirectory;
                while (--extentOfOffense >= 0 && targetDirectory?.Parent is { } nextTargetDirectory)
                    targetDirectory = nextTargetDirectory;
                if (targetDirectory is null)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I don't know how, but I sort of... ran out of folders figuring out where to move this mod. I think you're going to have to work this out on your own."), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderSwap;
                        options.Action = "Show me";
                        options.VisibleStateDuration = 30000;
                        options.Onclick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
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
                    superSnacks.OfferRefreshments(new MarkupString(
                        $"""
                        I want to move <strong>{file.Name}</strong> to <strong>{targetDirectory.FullName}</strong> in order for it to work properly.
                        That also means moving everything that's in the same folder so I don't break anything.
                        But, unfortunately, that would cause me to overwrite the following files or folders which already exist at the destination:
                        <br />
                        <br />
                        <ul>
                        {string.Join(Environment.NewLine, conflicts.Select(conflict => $"<li>&bull; {conflict.Name}</li>"))}
                        </ul>
                        <br />
                        I don't want to accidentally cause you to lose important files! You may need to sort this out for yourself. I'm sorry.
                        """), Severity.Error, options =>
                    {
                        options.Icon = MaterialDesignIcons.Normal.FolderAlert;
                        options.Action = "Show me";
                        options.VisibleStateDuration = 60000;
                        options.Onclick = _ =>
                        {
                            platformFunctions.ViewFile(file);
                            return Task.CompletedTask;
                        };
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
                    superSnacks.OfferRefreshments(new MarkupString(
                        $"""
                        Boy, did that *not* work. Your computer's operating system said:

                        `{moveEx.GetType().Name}: {moveEx.Message}`
                        """), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileAlert);
                    return Task.CompletedTask;
                }
                var newFile = new FileInfo(Path.Combine(targetDirectory.FullName, file.Name));
                superSnacks.OfferRefreshments(new MarkupString($"You got it, Chief. The file (and any of its pals) has been safely moved to a new home closer to the root of your Mods folder where the game shouldn't have any trouble with it."), Severity.Success, options =>
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
            if (resolutionCmd is "show")
            {
                var file = new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", modFilePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString("I couldn't do that because the file done wandered off."), Severity.Error, options => options.Icon = MaterialDesignIcons.Normal.FileQuestion);
                    return Task.CompletedTask;
                }
                platformFunctions.ViewFile(file);
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
        var anyLostInTheAbyss = false;
        await foreach (var offendingModFile in pbDbContext.ModFiles.Where(mf => mf.Path != null && mf.FileType == modsDirectoryFileType && mf.Path.Length - mf.Path.Replace("/", string.Empty).Replace("\\", string.Empty).Length > maximumDepth).AsAsyncEnumerable())
        {
            anyLostInTheAbyss = true;
            yield return GenerateSickScanIssue(new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", offendingModFile.Path!)), offendingModFile);
        }
        if (!anyLostInTheAbyss)
            yield return GenerateHealthyScanIssue();
    }

    protected abstract void StopScanning(IPlayer player);
}
