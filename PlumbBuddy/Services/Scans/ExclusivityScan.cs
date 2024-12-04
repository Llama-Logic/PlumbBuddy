namespace PlumbBuddy.Services.Scans;

public class ExclusivityScan :
    Scan,
    IExclusivityScan
{
    public ExclusivityScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings)
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
                settings.ScanForMutuallyExclusiveMods = false;
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var (exclusivity, conflictedMods) in pbDbContext.ModExclusivities
            .Where(me => me.SpecifiedByModFileManifests!.Count(mfm => mfm.ModFileHash!.ModFiles!.Any()) > 1)
            .Select(me => ValueTuple.Create
            (
                me.Name,
                me.SpecifiedByModFileManifests!
                    .Where(mfm => mfm.ModFileHash!.ModFiles!.Any())
                    .Select(mfm => new
                    {
                        mfm.Name,
                        Creators = mfm.Creators!.Select(c => c.Name).ToList(),
                        FilePaths = mfm.ModFileHash!.ModFiles!
                            .Select(mf => mf.Path!)
                            .ToList()
                    }).ToList()
            ))
            .AsAsyncEnumerable())
            yield return new()
            {
                Caption = string.Format(AppText.Scan_Exclusivity_ConflicingClaim_Caption, exclusivity),
                Description = string.Format(AppText.Scan_Exclusivity_ConflicingClaim_Description, string.Join(Environment.NewLine, conflictedMods.Select(mod => string.Format(AppText.Scan_Exclusivity_ConflicingClaim_Description_ListItem, mod.Name ?? AppText.Scan_Exclusivity_ConflicingClaim_Description_ModNameFallback, mod.Creators.Any() ? string.Format(AppText.Scan_Common_ByLine, mod.Creators.Humanize()) : string.Empty, mod.FilePaths.Select(filePath => $"`{filePath}`").Humanize())))),
                Icon = MaterialDesignIcons.Normal.Fencing,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = (exclusivity, conflictedMods),
                Resolutions =
                [
                    ..conflictedMods.SelectMany(mod => mod.FilePaths).Select((filePath, index) => new ScanIssueResolution()
                    {
                        Label = string.Format(AppText.Scan_Exclusivity_ConflicingClaim_ShowFile_Label, (index + 1).ToOrdinalWords()),
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = AppText.Scan_Common_StopTellingMe_Label,
                        CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                        CautionText = AppText.Scan_Exclusivity_ConflicingClaim_StopTellingMe_CautionText,
                        Data = "stopTellingMe"
                    }
                ]
            };
    }
}
