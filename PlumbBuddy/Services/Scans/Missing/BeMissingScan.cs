namespace PlumbBuddy.Services.Scans.Missing;

public sealed class BeMissingScan :
    MissingScan,
    IBeMissingScan
{
    public BeMissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings) :
        base(pbDbContextFactory, settings)
    {
    }

    protected override string GuideRedirectSuffix =>
        nameof(BeMissingScan);

    protected override string ModName =>
        AppText.Scan_Missing_ModName_BetterExceptions;

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=BetterExceptions", UriKind.Absolute);

    protected override string ModUtility =>
        AppText.Scan_Missing_Utility_BetterExceptions;

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } = null;

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "tmex_BetterExceptions.pyc"
    ];

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForMissingBe = false;
}
