namespace PlumbBuddy.Services.Scans.Missing;

public sealed class McccMissingScan :
    MissingScan,
    IMcccMissingScan
{
    public McccMissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings player) :
        base(pbDbContextFactory, player)
    {
    }

    protected override string ModName { get; } =
        "MC Command Center";

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=GetMCCommandCenter", UriKind.Absolute);

    protected override string ModUtility { get; } =
        "your Sims 4 game experience, NPC story progression options, and error logging";

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } =
    [
        "00b2D882:00000000:d1398f77942376fb"
    ];

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "mc_cmd_center.pyc"
    ];

    protected override void StopScanning(ISettings player) =>
        player.ScanForMissingMccc = false;
}
