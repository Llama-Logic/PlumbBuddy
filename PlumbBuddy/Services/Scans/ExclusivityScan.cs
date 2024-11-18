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
                Caption = $"Multiple Mods Claim \"{exclusivity}\"",
                Description =
                    $"""
                    "{exclusivity}" is a special thing that only one mod file in your Mods folder can do or have *for things to work correctly*, and unfortunately, you've installed multiple mods trying to claim it. It would be best to remove all but one of the whole mods these mod files are a part of, although you may want to review the **Catalog** to see what other mods are dependents of them to make an informed decision.<br />
                    *Note: Some mods may be listed as dependents of two or more of these mods in this execlusivity conflict. This **usually** means that they will be okay with just one of them, so you don't have to worry about an impossible choice in their case.*<br />
                    Here are the mod files each claiming \"{exclusivity}\":
                    {string.Join(Environment.NewLine, conflictedMods.Select(mod => $"* **{mod.Name ?? "Some Mod"}**{(mod.Creators.Any() ? $" by {mod.Creators.Humanize()}" : string.Empty)} located at {mod.FilePaths.Select(filePath => $"`{filePath}`").Humanize()}"))}
                    """,
                Icon = MaterialDesignIcons.Normal.Fencing,
                Type = ScanIssueType.Sick,
                Origin = this,
                Data = (exclusivity, conflictedMods),
                Resolutions =
                [
                    ..conflictedMods.SelectMany(mod => mod.FilePaths).Select((filePath, index) => new ScanIssueResolution()
                    {
                        Label = $"Show me the {(index + 1).ToOrdinalWords(new CultureInfo("en-US"))} file",
                        Icon = MaterialDesignIcons.Normal.FileFind,
                        Color = MudBlazor.Color.Secondary,
                        Data = $"showfile-{filePath}"
                    }),
                    new()
                    {
                        Icon = MaterialDesignIcons.Normal.Cancel,
                        Label = "Stop telling me",
                        CautionCaption = "Disable this scan?",
                        CautionText = "So the creators went to all this trouble to embed metadata so that I can tell you when you have conflicting mods installed and... you're just disinterested? Disable this scan all you want... won't fix the problem, though.",
                        Data = "stopTellingMe"
                    }
                ]
            };
    }
}
