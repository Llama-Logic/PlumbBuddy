namespace PlumbBuddy.Services.Scans.Corrupt;

public sealed class Ts4ScriptCorruptScan :
    CorruptScan,
    ITs4ScriptCorruptScan
{
    public Ts4ScriptCorruptScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, superSnacks, ModsDirectoryFileType.CorruptScriptArchive, 1)
    {
    }

    protected override ScanIssue GenerateDeadScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchRemove,
            Caption = $"A Mod File is Corrupt: {file.Name}",
            Description =
                $"""
                I found this corrupt script archive in your Mods folder, specifically at this path:
                `{modFile.Path}`<br />
                Your game is not going to be able to start with this file here. Let's move it to your Downloads folder, safely out of the game's reach.
                """,
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = modFile.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = "Move it to the Downloads folder",
                    Color = MudBlazor.Color.Primary,
                    Data = "moveToDownloads"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "I understand that this might be annoying, but this file is *really* no good. And turning this scan off doesn't change that. If you don't believe me, just trying launching your game right now. You're in for a rude surprise.",
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchCheck,
            Caption = "No Script Archives are Corrupt",
            Description = "I didn't find any corrupt script archives in your Mods folder. That's good because if I did, the game would crash if it found them.",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchRemove,
            Caption = $"A Mod File is Corrupt: {file.Name}",
            Description =
                $"""
                I found this corrupt script archive in your Mods folder, specifically at this path:
                `{modFile.Path}`<br />
                Thankfully, its also at an invalid depth for the game to find it. Still though, it's a time bomb waiting to go off. Let's move it to your Downloads folder, safely out of the game's reach.
                """,
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = modFile.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = "Move it to the Downloads folder",
                    Color = MudBlazor.Color.Primary,
                    Data = "moveToDownloads"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "I understand that this might be annoying, but this file is *really* no good. And turning this scan off doesn't change that. All it will do is hide this warning so that you can get a very nasty surprise from the game later.",
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForCorruptScriptMods = false;
}
