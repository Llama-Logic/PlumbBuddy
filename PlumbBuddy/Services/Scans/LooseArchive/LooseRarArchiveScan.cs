namespace PlumbBuddy.Services.Scans.LooseArchive;

public sealed class LooseRarArchiveScan :
    LooseArchiveScan,
    ILooseRarArchiveScan
{
    public LooseRarArchiveScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, superSnacks, ModsDirectoryFileType.RarArchive)
    {
    }

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.ZipBox,
            Caption = "No RAR Files",
            Description = "I didn't find any RAR files in your Mods folder, which is a good thing. It's best to keep them out of there!",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.ZipBox,
            Caption = $"I Found a RAR File: {file.Name}",
            Description =
                $"""
                I found this RAR file in your Mods folder, specifically at this path:
                `{fileOfInterest.Path}`<br />
                While it's not *technically* causing a problem, it makes me uncomfortable since it can't be used by the game in there and it could trick you into thinking you've installed a mod when you really haven't.
                """,
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = fileOfInterest.Path,
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
                    CautionText = "I mean, I get that this is nit picky, but it's bad Mods folder hygiene to have that RAR file in there. And turning off this scan is just hiding this warning about it, not addressing the root cause.",
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForLooseRarArchives = false;
}
