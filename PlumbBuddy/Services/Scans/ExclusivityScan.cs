namespace PlumbBuddy.Services.Scans;

public class ExclusivityScan :
    Scan,
    IExclusivityScan
{
    public ExclusivityScan(IPlatformFunctions platformFunctions, IPlayer player, PbDbContext pbDbContext)
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
        }
        return Task.CompletedTask;
    }

    public override IAsyncEnumerable<ScanIssue> ScanAsync()
    {
        return base.ScanAsync();
    }
}
