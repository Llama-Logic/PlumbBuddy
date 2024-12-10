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
            Caption = AppText.Scan_LooseArchive_Rar_NoneFound_Caption,
            Description = AppText.Scan_LooseArchive_Rar_NoneFound_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.ZipBox,
            Caption = string.Format(AppText.Scan_LooseArchive_Rar_Found_Caption, file.Name),
            Description = string.Format(AppText.Scan_LooseArchive_Rar_Found_Description, fileOfInterest.Path),
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = fileOfInterest.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = AppText.Scan_LooseArchive_MoveToDownloads_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = "moveToDownloads"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FileFind,
                    Label = AppText.Scan_Common_ShowMeThisFile_Label,
                    Color = MudBlazor.Color.Secondary,
                    Data = "show"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_LooseArchive_Rar_StopTellingMe_CautionText,
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForLooseRarArchives = false;
}
