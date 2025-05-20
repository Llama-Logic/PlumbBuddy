namespace PlumbBuddy.Services.Scans.Missing;

public sealed class McccMissingScan :
    MissingScan,
    IMcccMissingScan
{
    public McccMissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings) :
        base(pbDbContextFactory, settings)
    {
    }

    protected override string GuideRedirectSuffix =>
        nameof(McccMissingScan);

    protected override string ModName =>
        AppText.Scan_Missing_ModName_MCCommandCenter;

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=GetMCCommandCenter", UriKind.Absolute);

    protected override string ModUtility =>
        AppText.Scan_Missing_Utility_MCCommandCenter;

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } =
    [
        "00b2D882:00000000:d1398f77942376fb"
    ];

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "mc_cmd_center.pyc"
    ];

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForMissingMccc = false;
}
