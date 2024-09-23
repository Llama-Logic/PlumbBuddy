namespace PlumbBuddy.Services.Scans.Missing;

public sealed class BeMissingScan :
    MissingScan,
    IBeMissingScan
{
    public BeMissingScan(PbDbContext pbDbContext, IPlayer player) :
        base(pbDbContext, player)
    {
    }

    protected override string ModName { get; } = "Better Exceptions";

    protected override Uri ModUrl { get; } = new("https://www.patreon.com/posts/99403318", UriKind.Absolute);

    protected override string ModUtility => "detailed exception, mod conflict, and patch day analyses";

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } = null;

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "tmex_BetterExceptions.pyc"
    ];

    protected override void StopScanning(IPlayer player) =>
        player.ScanForMissingBe = false;
}
