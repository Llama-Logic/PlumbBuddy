namespace PlumbBuddy.Services.Scans.Missing;

public sealed class BeMissingScan :
    MissingScan,
    IBeMissingScan
{
    public BeMissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings player) :
        base(pbDbContextFactory, player)
    {
    }

    protected override string ModName { get; } =
        "Better Exceptions";

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=BetterExceptions", UriKind.Absolute);

    protected override string ModUtility =>
        "detailed exception, mod conflict, and patch day analyses";

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } = null;

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "tmex_BetterExceptions.pyc"
    ];

    protected override void StopScanning(ISettings player) =>
        player.ScanForMissingBe = false;
}
