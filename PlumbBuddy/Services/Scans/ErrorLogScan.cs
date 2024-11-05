namespace PlumbBuddy.Services.Scans;

public sealed class ErrorLogScan :
    Scan,
    IErrorLogScan
{
    public ErrorLogScan(ILogger<ErrorLogScan> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings player, IPublicCatalogs publicCatalogs, IBlazorFramework blazorFramework)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        ArgumentNullException.ThrowIfNull(blazorFramework);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.publicCatalogs = publicCatalogs;
        this.blazorFramework = blazorFramework;
    }

    readonly IBlazorFramework blazorFramework;
    readonly ILogger<ErrorLogScan> logger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings player;
    readonly IPublicCatalogs publicCatalogs;

    public override async Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string command)
        {
            if (command is "stopTellingMe")
            {
                player.ScanForErrorLogs = false;
                return;
            }
            if (issueData is string userDataRelativePath)
            {
                var file = new FileInfo(Path.Combine(player.UserDataFolderPath, userDataRelativePath));
                if (file.Exists)
                {
                    if (command is "delete")
                    {
                        file.Delete();
                        return;
                    }
                    if (command is "discord")
                    {
                        await blazorFramework.MainLayoutLifetimeScope!.Resolve<IDialogService>().AskForHelpAsync(logger, publicCatalogs, file).ConfigureAwait(false);
                        return;
                    }
                    if (command is "show")
                    {
                        platformFunctions.ViewFile(file);
                        return;
                    }
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
                Caption = player.Type is UserType.Casual
                    ? "The Game or one of Your Mods is Calling for Help!"
                    : "A File Likely Containing an Error Has Been Found",
                Description =
                    player.Type is UserType.Casual
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
                        Label = "Show me the file",
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Data = "show"
                    },
                    new()
                    {
                        Label = "Delete the file",
                        Icon = MaterialDesignIcons.Normal.FileCancel,
                        Color = MudBlazor.Color.Warning,
                        Data = "delete",
                        CautionCaption = "Are you sure?",
                        CautionText = "This file may contain important information about a problem with your Sims 4 setup, and it might even contain something critical which would help others, too. Once I delete it, all that potential will be gone forever."
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
