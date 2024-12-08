namespace PlumbBuddy.Services.Scans.Missing;

public sealed class ModGuardMissingScan :
    MissingScan,
    IModGuardMissingScan
{
    public ModGuardMissingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings) :
        base(pbDbContextFactory, settings)
    {
    }

    protected override string ModName =>
        AppText.Scan_Missing_ModName_ModGuard;

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=ModGuard", UriKind.Absolute);

    protected override string ModUtility =>
        AppText.Scan_Missing_Utility_ModGuard;

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } =
        null;

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "tmex-ModGuard.pyc"
    ];

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForMissingModGuard = false;
}
