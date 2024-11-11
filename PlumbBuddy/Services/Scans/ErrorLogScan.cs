namespace PlumbBuddy.Services.Scans;

public sealed class ErrorLogScan :
    Scan,
    IErrorLogScan
{
    public ErrorLogScan(ILogger<ErrorLogScan> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, IPublicCatalogs publicCatalogs, IBlazorFramework blazorFramework, ISmartSimObserver smartSimObserver)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.publicCatalogs = publicCatalogs;
        this.blazorFramework = blazorFramework;
        this.smartSimObserver = smartSimObserver;
    }

    readonly IBlazorFramework blazorFramework;
    readonly ILogger<ErrorLogScan> logger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly IPublicCatalogs publicCatalogs;
    readonly ISmartSimObserver smartSimObserver;

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string command)
        {
            if (command is "stopTellingMe")
            {
                settings.ScanForErrorLogs = false;
                return;
            }
            if (issueData is string userDataRelativePath)
            {
                var dialogService = blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>();
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, userDataRelativePath));
                if (file.Exists)
                {
                    if (command is "delete")
                    {
                        var foundErrorLogs = smartSimObserver.ScanIssues
                            .Where(si => si.Origin is IErrorLogScan && si.Data is string)
                            .Select(si => (string)si.Data!)
                            .ToImmutableArray();
                        if (foundErrorLogs.Length is 1)
                        {
                            if (await dialogService.ShowCautionDialogAsync("Are you sure?", "This file may contain important information about a problem with your Sims 4 setup, and it might even contain something critical which would help others, too. Once I delete it, all that potential will be gone forever.").ConfigureAwait(false))
                                file.Delete();
                            return;
                        }
                        if (await dialogService.ShowDeleteErrorLogsDialogAsync(foundErrorLogs, [userDataRelativePath]).ConfigureAwait(false) is { } deleteErrorLogsPaths)
                        {
                            foreach (var deleteErrorLogsPath in deleteErrorLogsPaths)
                            {
                                var deleteErrorLog = new FileInfo(Path.Combine(settings.UserDataFolderPath, deleteErrorLogsPath));
                                if (deleteErrorLog.Exists)
                                {
                                    try
                                    {
                                        deleteErrorLog.Delete();
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                        return;
                    }
                    if (command is "discord")
                    {
                        await dialogService.ShowAskForHelpDialogAsync(logger, publicCatalogs, file).ConfigureAwait(false);
                        return;
                    }
                    if (command is "show")
                    {
                        platformFunctions.ViewFile(file);
                        return;
                    }
                }
                else
                {
                    await dialogService.ShowInfoDialogAsync("Whoops, that file has run off...",
                        """
                        I'm sorry, but it's just not there any more. I'm gonna go ahead and refresh this screen for you.
                        """);
                    using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    await pbDbContext.FilesOfInterest
                        .Where(foi => foi.Path == userDataRelativePath)
                        .ExecuteDeleteAsync().ConfigureAwait(false);
                    smartSimObserver.Scan();
                }
            }
        }
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var errorFilePath in pbDbContext.FilesOfInterest
            .Where(foi => (foi.FileType == ModsDirectoryFileType.TextFile || foi.FileType == ModsDirectoryFileType.HtmlFile)
                && (foi.Path.ToLower().Contains("exception") || foi.Path.ToLower().Contains("crash")))
            .Select(foi => foi.Path)
            .AsAsyncEnumerable())
            yield return new()
            {
                Caption = settings.Type is UserType.Casual
                    ? "The Game or one of Your Mods is Calling for Help!"
                    : "A File Likely Containing an Error Has Been Found",
                Description =
                    settings.Type is UserType.Casual
                    ?
                    $"""
                    I found this file in your User Data folder and its presence is a signal that something went wrong.<br />
                    `{errorFilePath}`<br />
                    We could take the file to informed people in Discord who can help us figure out what it means and if there's anything we should do about it.
                    """
                    :
                    $"""
                    A file which is likely an exception log or technical report triggered by an error was found at the following path in your User Data folder:<br />
                    `{errorFilePath}`
                    """,
                Icon = MaterialDesignIcons.Normal.FileDocumentAlert,
                Type = ScanIssueType.Uncomfortable,
                Origin = this,
                Data = errorFilePath,
                Resolutions =
                [
                    new()
                    {
                        Label = "Select a Sims 4 Community Discord",
                        Icon = MaterialDesignIcons.Normal.FaceAgent,
                        Color = MudBlazor.Color.Primary,
                        Data = "discord"
                    },
                    new()
                    {
                        Label = "Show me this file",
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Data = "show"
                    },
                    new()
                    {
                        Label = "Delete error files",
                        Icon = MaterialDesignIcons.Normal.FileCancel,
                        Color = MudBlazor.Color.Warning,
                        Data = "delete"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = "If you're experiencing trepidation about confronting this file or speaking with strangers about it, I understand. But if you tell me to choose a Discord, I'll be here with you the whole time to help you through the process. Disabling this scan will just hide the problem; it won't solve it.",
                        Data = "stopTellingMe"
                    }
                ]
            };
    }
}
