namespace PlumbBuddy.Services.Scans.LooseArchive;

public sealed class LooseZipArchiveScan :
    LooseArchiveScan,
    ILooseZipArchiveScan
{
    public LooseZipArchiveScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, superSnacks, ModsDirectoryFileType.ZipArchive)
    {
    }

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderZip,
            Caption = AppText.Scan_LooseArchive_Zip_NoneFound_Caption,
            Description = AppText.Scan_LooseArchive_Zip_NoneFound_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, FileOfInterest fileOfInterest) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderZip,
            Caption = string.Format(AppText.Scan_LooseArchive_Zip_Found_Caption, file.Name),
            Description = string.Format(AppText.Scan_LooseArchive_Zip_Found_Description, fileOfInterest.Path),
            Origin = this,
            Type = ScanIssueType.Sick,
            Data = fileOfInterest.Path,
            GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthLooseZipArchiveScan{settings.Type}", UriKind.Absolute),
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
                    CautionText = AppText.Scan_LooseArchive_Zip_StopTellingMe_CautionText,
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForLooseZipArchives = false;
}
