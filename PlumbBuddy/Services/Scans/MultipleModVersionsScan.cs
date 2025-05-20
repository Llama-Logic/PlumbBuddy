namespace PlumbBuddy.Services.Scans;

public sealed class MultipleModVersionsScan :
    Scan,
    IMultipleModVersionsScan
{
    public MultipleModVersionsScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
    }

    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string resolutionStr)
        {
            if (resolutionStr.StartsWith("showfile-") && new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", resolutionStr[9..])) is { } modFile && modFile.Exists)
                platformFunctions.ViewFile(modFile);
            else if (resolutionStr is "stopTellingMe")
                settings.ScanForMultipleModVersions = false;
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var uniqueConflictedFilesSignatures = new HashSet<string>();
        foreach (var modFileManfiestHashId in await pbDbContext.ModFileManifestHashes
            .Where(mfmh => mfmh.ManifestsByCalculation.Concat(mfmh.ManifestsBySubsumption).Any(mfm => mfm.ModFileHash.ModFiles.Select(mf => mf.Path).Distinct().Count() > 1))
            .Select(mfmh => mfmh.Id)
            .ToListAsync().ConfigureAwait(false))
        {
            var duplicates = await pbDbContext.ModFileManifests
                .Where(mfm => mfm.ModFileHash.ModFiles.Any() && (mfm.CalculatedModFileManifestHashId == modFileManfiestHashId || mfm.SubsumedHashes.Any(sh => sh.Id == modFileManfiestHashId)))
                .Select(mfm => new
                {
                    mfm.Name,
                    Creators = mfm.Creators.Select(c => c.Name).ToList(),
                    mfm.Url,
                    mfm.Version,
                    FilePaths = mfm.ModFileHash.ModFiles.Select(mf => mf.Path).ToList()
                })
                .ToListAsync().ConfigureAwait(false);
            if (!uniqueConflictedFilesSignatures.Add(string.Join("|", duplicates.SelectMany(d => d.FilePaths).Distinct().Order())))
                continue;
            var distinctNames = duplicates.Select(mod => mod.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToImmutableArray();
            var distinctUrls = duplicates.Select(mod => mod.Url).Where(url => url is not null).Distinct().ToImmutableArray();
            var urlResolutions = new List<ScanIssueResolution>();
            if (distinctUrls.Length is 1)
                urlResolutions.Add(new()
                {
                    Label = string.Format(AppText.Scan_MultipleModVersions_Download_Label, distinctNames.Length is 1 ? string.Format(AppText.Scan_MultipleModVersions_Download_Label_ModNameFormat, distinctNames[0]) : string.Empty),
                    Icon = MaterialDesignIcons.Normal.Web,
                    Color = MudBlazor.Color.Primary,
                    Data = $"visit-mod-download",
                    Url = distinctUrls[0]
                });
            else
                urlResolutions.AddRange
                (
                    duplicates.Select((duplicate, index) => new ScanIssueResolution()
                    {
                        Label = string.Format(AppText.Scan_MultipleModVersions_DownloadOneOfMultiple_Label, (index + 1).ToOrdinalWords()),
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"visit-mod-{index}",
                        Url = duplicate.Url
                    })
                );
            yield return new()
            {
                Caption = string.Format(AppText.Scan_MultipleModVersions_Caption, distinctNames.Length is 1 ? distinctNames[0] : AppText.Scan_MultipleModVersions_Caption_ModNameFallback),
                Description = string.Format(AppText.Scan_MultipleModVersions_Description, string.Join(Environment.NewLine, duplicates.Select(mod => string.Format(AppText.Scan_MultipleModVersions_Description_ListItem, mod.Name ?? AppText.Scan_MultipleModVersions_Description_ListItem_ModNameFallback, string.IsNullOrWhiteSpace(mod.Version) ? string.Empty : string.Format(AppText.Scan_MultipleModVersions_Description_ListItem_VersionFormat, mod.Version), mod.Creators.Any() ? string.Format(AppText.Scan_MultipleModVersions_Description_ListItem_ByLineFormat, mod.Creators.Humanize()) : string.Empty, mod.FilePaths.Select(filePath => $"`{filePath}`").Humanize())))),
                Icon = MaterialDesignIcons.Normal.TimelineAlert,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modFileManfiestHashId,
                GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthMultipleModVersionsScan{settings.Type}", UriKind.Absolute),
                Resolutions =
                [
                    ..urlResolutions,
                    ..duplicates.SelectMany(mod => mod.FilePaths).Select((filePath, index) => new ScanIssueResolution()
                    {
                        Label = string.Format(AppText.Scan_MultipleModVersions_ShowOneOfMultiple_Label, (index + 1).ToOrdinalWords()),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_MultipleModVersions_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
    }
}
