namespace PlumbBuddy.Services.Scans.LooseArchive;

public sealed class LooseZipArchiveScan :
    LooseArchiveScan,
    ILooseZipArchiveScan
{
    public LooseZipArchiveScan(IPlatformFunctions platformFunctions, PbDbContext pbDbContext, IPlayer player, ISuperSnacks superSnacks) :
        base(platformFunctions, pbDbContext, player, superSnacks, ModsDirectoryFileType.ZipArchive)
    {
    }

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderZip,
            Caption = "No ZIP Files",
            Description = "I didn't find any ZIP files in your Mods folder, which is a good thing. It's best to keep them out of there!",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderZip,
            Caption = $"I Found a ZIP File: {file.Name}",
            Description =
                $"""
                I found this ZIP file in your Mods folder, specifically at this path:

                `{file.FullName}`

                You may believe that it's not *technically* causing a problem, but it has been reported that deprecated code paths in the game may attempt to open this file, which would be <strong>very bad</strong>. Let's move it to your Downloads folder right away.
                """,
            Origin = this,
            Type = ScanIssueType.Sick,
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
                    CautionText = "Look, this ZIP file being in your Mods folder can't do any good and it just might do harm. Turning off this scan is just hiding this warning about it, not prevent that potential harm.",
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(IPlayer player) =>
        player.ScanForLooseZipArchives = false;
}
