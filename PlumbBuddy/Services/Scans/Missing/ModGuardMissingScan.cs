namespace PlumbBuddy.Services.Scans.Missing;

public sealed class ModGuardMissingScan :
    MissingScan,
    IModGuardMissingScan
{
    public ModGuardMissingScan(PbDbContext pbDbContext, IPlayer player) :
        base(pbDbContext, player)
    {
    }

    protected override string ModName { get; } =
        "ModGuard";

    protected override Uri ModUrl { get; } =
        new("https://plumbbuddy.app/redirect?to=ModGuard", UriKind.Absolute);

    protected override string ModUtility { get; } =
        "protection from hacking, phishing, and other malware attacks which may occur if a modder's online accounts are compromised";

    protected override IReadOnlyList<ResourceKey>? RequiredPackageKeys { get; } =
        null;

    protected override IReadOnlyList<string>? RequiredScriptArchiveFullNames { get; } =
    [
        "tmex-ModGuard.pyc"
    ];

    protected override void StopScanning(IPlayer player) =>
        player.ScanForMissingModGuard = false;
}
