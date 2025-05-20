namespace PlumbBuddy.Services.Scans;

public sealed class MismatchedInscribedHashesScan :
    Scan,
    IMismatchedInscribedHashesScan
{
    public MismatchedInscribedHashesScan(IPlatformFunctions platformFunctions, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IUserInterfaceMessaging userInterfaceMessaging)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.userInterfaceMessaging = userInterfaceMessaging;
    }

    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    readonly IUserInterfaceMessaging userInterfaceMessaging;

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string resolutionStr)
        {
            if (resolutionStr.StartsWith("download-") && Uri.TryCreate(resolutionStr[9..], UriKind.Absolute, out var url))
                Browser.OpenAsync(url.ToString(), BrowserLaunchMode.External);
            else if (resolutionStr.StartsWith("updateManifest-"))
                userInterfaceMessaging.BeginManifestingMod(resolutionStr[15..]);
            else if (resolutionStr.StartsWith("showfile-") && new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", resolutionStr[9..])) is { } modFile && modFile.Exists)
                platformFunctions.ViewFile(modFile);
            else if (resolutionStr is "stopTellingMe")
                settings.ScanForMismatchedInscribedHashes = false;
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var mod in pbDbContext.ModFileManifests
            .Where(mfm => mfm.ModFileHash.ModFiles.Any() && mfm.CalculatedModFileManifestHashId != mfm.InscribedModFileManifestHashId)
            .Select(mfm => new
            {
                mfm.Name,
                Creators = mfm.Creators.Select(c => c.Name).ToList(),
                mfm.Url,
                FilePaths = mfm.ModFileHash.ModFiles
                    .Select(mf => mf.Path!)
                    .ToList()
            })
            .AsAsyncEnumerable())
            yield return new()
            {
                Caption = string.Format(AppText.Scan_MismatchedInscribedHashes_Caption, string.IsNullOrWhiteSpace(mod.Name) ? AppText.Scan_MismatchedInscribedHashes_Caption_ModNameFallback : mod.Name),
                Description = string.Format(AppText.Scan_MismatchedInscribedHashes_Description, string.IsNullOrWhiteSpace(mod.Name) ? AppText.Scan_MismatchedInscribedHashes_Caption_ModNameFallback : mod.Name, mod.Creators.Any() ? string.Format(AppText.Scan_Common_ByLine, mod.Creators.Humanize()) : string.Empty),
                Icon = MaterialDesignIcons.Normal.BarcodeOff,
                Type = ScanIssueType.Dead,
                Origin = this,
                Data = mod,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthMismatchedInscribedHashesScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..settings.Type is UserType.Creator
                        && await userInterfaceMessaging.IsModScaffoldedAsync(mod.FilePaths.First()).ConfigureAwait(false)
                        ? [new ScanIssueResolution()
                        {
                            Label = AppText.Scan_MismatchedInscribedHashes_UpdateManifest_Label,
                            Icon = MaterialDesignIcons.Normal.TagEdit,
                            Color = MudBlazor.Color.Tertiary,
                            Data = $"updateManifest-{mod.FilePaths.First()}"
                        }]
                        : mod.Url is { } url
                        ? [new ScanIssueResolution()
                        {
                            Label = AppText.Scan_MismatchedInscribedHashes_Download_Label,
                            Icon = MaterialDesignIcons.Normal.Web,
                            Color = MudBlazor.Color.Primary,
                            Data = $"download-{url}"
                        }]
                        : Enumerable.Empty<ScanIssueResolution>(),
                    ..mod.FilePaths.Select((filePath, index) => new ScanIssueResolution()
                    {
                        Label = string.Format(AppText.Scan_MismatchedInscribedHashes_ShowFile_Label, (index + 1).ToOrdinalWords()),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_MismatchedInscribedHashes_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
    }
}
