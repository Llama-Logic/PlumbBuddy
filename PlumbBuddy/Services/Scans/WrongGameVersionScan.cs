namespace PlumbBuddy.Services.Scans;

public class WrongGameVersionScan :
    Scan,
    IWrongGameVersionScan
{
    public WrongGameVersionScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.superSnacks = superSnacks;
    }

    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (issueData is string corruptModFilePath && resolutionData is string resolutionCmd)
        {
            if (resolutionCmd is "moveToDownloads")
            {
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", corruptModFilePath));
                if (!file.Exists)
                {
                    superSnacks.OfferRefreshments(new MarkupString(AppText.Scan_Corrupt_MoveToDownloads_Error_CannotFind), Severity.Error, options =>
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
                try
                {
                    file.MoveTo(prospectiveTargetPath);
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
                var newFile = new FileInfo(prospectiveTargetPath);
                superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Scan_Corrupt_MoveToDownloads_Success, newFile.Name)), Severity.Success, options =>
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
                var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", corruptModFilePath));
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
                settings.ScanForWrongGameVersion = false;
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        var anyCheeseSlidOffTheCracker = false;
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var record in pbDbContext.ModFiles
            .Where(mf => mf.FoundAbsent == null && mf.FileType == ModsDirectoryFileType.Package && (mf.ModFileHash.DataBasePackedFileMajorVersion != 2 || mf.ModFileHash.DataBasePackedFileMinorVersion != 1))
            .Select(mf => new { ModFile = mf, mf.ModFileHash })
            .AsAsyncEnumerable())
        {
            anyCheeseSlidOffTheCracker = true;
            var modFile = record.ModFile;
            var modFileHash = record.ModFileHash;
            var file = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", modFile.Path!));
            if (modFileHash.DataBasePackedFileMajorVersion is 1
                && modFileHash.DataBasePackedFileMinorVersion is 1)
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
                    Caption = string.Format(AppText.Scan_WrongGameVersion_TS2PackageFound_Caption, file.Name),
                    Description = string.Format(AppText.Scan_WrongGameVersion_TS2PackageFound_Description, modFile.Path),
                    Origin = this,
                    Type = ScanIssueType.Dead,
                    Data = modFile.Path,
                    GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthOtherGameScan{settings.Type}", UriKind.Absolute),
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FolderMove,
                            Label = AppText.Scan_Corrupt_Found_Move_Label,
                            Color = MudBlazor.Color.Primary,
                            Data = "moveToDownloads"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FileFind,
                            Label = AppText.Scan_Common_ShowMeThisFile_Label,
                            Color = MudBlazor.Color.Secondary,
                            Data = "show"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                            Data = "stopTellingMe"
                        }
                    ]
                };
            else if (modFileHash.DataBasePackedFileMajorVersion is 2
                && modFileHash.DataBasePackedFileMinorVersion is 0)
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
                    Caption = string.Format(AppText.Scan_WrongGameVersion_TS3PackageFound_Caption, file.Name),
                    Description = string.Format(AppText.Scan_WrongGameVersion_TS3PackageFound_Description, modFile.Path),
                    Origin = this,
                    Type = ScanIssueType.Uncomfortable,
                    Data = modFile.Path,
                    GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthOtherGameScan{settings.Type}", UriKind.Absolute),
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FolderMove,
                            Label = AppText.Scan_Corrupt_Found_Move_Label,
                            Color = MudBlazor.Color.Primary,
                            Data = "moveToDownloads"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FileFind,
                            Label = AppText.Scan_Common_ShowMeThisFile_Label,
                            Color = MudBlazor.Color.Secondary,
                            Data = "show"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                            Data = "stopTellingMe"
                        }
                    ]
                };
            else if (modFileHash.DataBasePackedFileMajorVersion is 3
                && modFileHash.DataBasePackedFileMinorVersion is 0)
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
                    Caption = string.Format(AppText.Scan_WrongGameVersion_SC5PackageFound_Caption, file.Name),
                    Description = string.Format(AppText.Scan_WrongGameVersion_SC5PackageFound_Description, modFile.Path),
                    Origin = this,
                    Type = ScanIssueType.Uncomfortable,
                    Data = modFile.Path,
                    GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthOtherGameScan{settings.Type}", UriKind.Absolute),
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FolderMove,
                            Label = AppText.Scan_Corrupt_Found_Move_Label,
                            Color = MudBlazor.Color.Primary,
                            Data = "moveToDownloads"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FileFind,
                            Label = AppText.Scan_Common_ShowMeThisFile_Label,
                            Color = MudBlazor.Color.Secondary,
                            Data = "show"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                            Data = "stopTellingMe"
                        }
                    ]
                };
            else
                yield return new()
                {
                    Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
                    Caption = string.Format(AppText.Scan_WrongGameVersion_PackageFound_Caption, file.Name),
                    Description = string.Format(AppText.Scan_WrongGameVersion_PackageFound_Description, modFile.Path),
                    Origin = this,
                    Type = ScanIssueType.Dead,
                    Data = modFile.Path,
                    GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthOtherGameScan{settings.Type}", UriKind.Absolute),
                    Resolutions =
                    [
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FolderMove,
                            Label = AppText.Scan_Corrupt_Found_Move_Label,
                            Color = MudBlazor.Color.Primary,
                            Data = "moveToDownloads"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.FileFind,
                            Label = AppText.Scan_Common_ShowMeThisFile_Label,
                            Color = MudBlazor.Color.Secondary,
                            Data = "show"
                        },
                        new()
                        {
                            Icon = MaterialDesignIcons.Normal.Cancel,
                            Label = AppText.Scan_Common_StopTellingMe_Label,
                            CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                            CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                            Data = "stopTellingMe"
                        }
                    ]
                };
        }
        if (!anyCheeseSlidOffTheCracker)
            yield return new()
            {
                Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
                Caption = AppText.Scan_WrongGameVersion_NoneFound_Caption,
                Description = AppText.Scan_WrongGameVersion_NoneFound_Description,
                Origin = this,
                Type = ScanIssueType.Healthy
            };
    }
}
