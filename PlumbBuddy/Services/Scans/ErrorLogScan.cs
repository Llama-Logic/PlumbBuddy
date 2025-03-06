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
                            if (await dialogService.ShowCautionDialogAsync(AppText.Scan_ErrorLog_Delete_Caution_Caption, AppText.Scan_ErrorLog_Delete_Caution_Text).ConfigureAwait(false))
                            {
                                file.Refresh();
                                try
                                {
                                    file.Delete();
                                }
                                catch
                                {
                                }
                            }
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
                    await dialogService.ShowInfoDialogAsync(AppText.Scan_ErrorLog_Delete_Failure_Caption, AppText.Scan_ErrorLog_Delete_Failure_Text);
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
                    ? AppText.Scan_ErrorLog_FileFound_Caption_Casual
                    : AppText.Scan_ErrorLog_FileFound_Caption_NonCasual,
                Description =
                    settings.Type is UserType.Casual
                    ? string.Format(AppText.Scan_ErrorLog_FileFound_Text_Casual, errorFilePath)
                    : string.Format(AppText.Scan_ErrorLog_FileFound_Text_NonCasual, errorFilePath),
                Icon = MaterialDesignIcons.Normal.FileDocumentAlert,
                Type = ScanIssueType.Uncomfortable,
                Origin = this,
                Data = errorFilePath,
                Resolutions =
                [
                    new()
                    {
                        Label = AppText.Scan_ErrorLog_FileFound_SelectSupportDiscord_Label,
                        Icon = MaterialDesignIcons.Normal.FaceAgent,
                        Color = MudBlazor.Color.Primary,
                        Data = "discord"
                    },
                    new()
                    {
                        Label = AppText.Scan_ErrorLog_FileFound_ShowFile_Label,
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Data = "show"
                    },
                    new()
                    {
                        Label = AppText.Scan_ErrorLog_FileFound_Delete_Label,
                        Icon = MaterialDesignIcons.Normal.FileCancel,
                        Color = MudBlazor.Color.Warning,
                        Data = "delete"
                    },
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_ErrorLog_FileFound_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
    }
}
