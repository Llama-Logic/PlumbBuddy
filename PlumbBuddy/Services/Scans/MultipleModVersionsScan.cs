namespace PlumbBuddy.Services.Scans;

public sealed class MultipleModVersionsScan :
    Scan,
    IMultipleModVersionsScan
{
    public MultipleModVersionsScan(IPlatformFunctions platformFunctions, IPlayer player, PbDbContext pbDbContext)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(pbDbContext);
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.pbDbContext = pbDbContext;
    }

    readonly PbDbContext pbDbContext;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;

    public override Task ResolveIssueAsync(object issueData, object resolutionData)
    {
        if (resolutionData is string resolutionStr)
        {
            if (resolutionStr.StartsWith("showfile-") && new FileInfo(Path.Combine(player.UserDataFolderPath, "Mods", resolutionStr[9..])) is { } modFile && modFile.Exists)
                platformFunctions.ViewFile(modFile);
            else if (resolutionStr is "stopTellingMe")
                player.ScanForMultipleModVersions = false;
        }
        return Task.CompletedTask;
    }

    public override async IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        foreach (var modFileManfiestHashId in await pbDbContext.ModFileManifestHashes
            .Where(mfmh => mfmh.ManifestsByCalculation!.Concat(mfmh.ManifestsBySubsumption!).Distinct().Sum(mfm => mfm.ModFileHash!.ModFiles!.Count(mf => mf.Path != null && mf.AbsenceNoticed == null)) > 1)
            .Select(mfmh => mfmh.Id)
            .ToListAsync().ConfigureAwait(false))
        {
            var duplicates = await pbDbContext.ModFileManifests
                .Where(mfm => mfm.ModFileHash!.ModFiles!.Any(mf => mf.Path != null && mf.AbsenceNoticed == null) && (mfm.CalculatedModFileManifestHashId == modFileManfiestHashId || mfm.SubsumedHashes!.Any(sh => sh.Id == modFileManfiestHashId)))
                .Select(mfm => new
                {
                    mfm.Name,
                    Creators = mfm.Creators!.Select(c => c.Name).ToList(),
                    mfm.Url,
                    mfm.Version,
                    FilePaths = mfm.ModFileHash!.ModFiles!.Where(mf => mf.Path != null && mf.AbsenceNoticed == null).Select(mf => mf.Path!).ToList()
                })
                .ToListAsync().ConfigureAwait(false);
            var distinctNames = duplicates.Select(mod => mod.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToImmutableArray();
            var distinctUrls = duplicates.Select(mod => mod.Url).Where(url => url is not null).Distinct().ToImmutableArray();
            var urlResolutions = new List<ScanIssueResolution>();
            if (distinctUrls.Length is 1)
                urlResolutions.Add(new()
                {
                    Label = $"Go to the Download Page{(distinctNames.Length is 1 ? $" for {distinctNames[0]}" : string.Empty)}",
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
                        Label = $"Go to the Download Page for the {(index + 1).ToOrdinalWords()} version",
                        Icon = MaterialDesignIcons.Normal.Web,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"visit-mod-{index}",
                        Url = duplicate.Url
                    })
                );
            yield return new()
            {
                Caption = $"I Found Multiple Versions of {(distinctNames.Length is 1 ? distinctNames[0] : "the Same Mod")} Installed",
                Description =
                    $"""
                    *First rule of government spending: why have one when you could have two at twice the price, eh?* Well, in this case, the price is **a broken mod**. I don't mean to be pushy, but you need to remove all but one of these.<br /><br />
                    {string.Join(Environment.NewLine, duplicates.Select(mod => $"* **{mod.Name ?? "Some Mod"}**{(string.IsNullOrWhiteSpace(mod.Version) ? string.Empty : $" ({mod.Version})")}{(mod.Creators.Any() ? $" by {mod.Creators.Humanize()}" : string.Empty)} located at {mod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()}"))}
                    """,
                Icon = MaterialDesignIcons.Normal.TimelineAlert,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = modFileManfiestHashId,
                Resolutions =
                [
                    ..urlResolutions,
                    ..duplicates.SelectMany(mod => mod.FilePaths).Select((filePath, index) => new ScanIssueResolution()
                    {
                        Label = $"Show me the {(index + 1).ToOrdinalWords()} file",
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = "So the creators went to all this trouble to embed metadata so that I can do all this complex hash set calculation to know when you've installed the same thing and twice *and will have problems as a result*... and you can't be bothered?",
                        Data = "stopTellingMe"
                    }
                ]
            };
        }
    }
}
