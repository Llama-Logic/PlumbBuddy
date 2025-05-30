namespace PlumbBuddy.Services.Scans.Corrupt;

public sealed class PackageCorruptScan :
    CorruptScan,
    IPackageCorruptScan
{
    public PackageCorruptScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, superSnacks, ModsDirectoryFileType.CorruptPackage, 5)
    {
    }

    protected override ScanIssue GenerateDeadScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
            Caption = string.Format(AppText.Scan_Corrupt_Found_Caption, file.Name),
            Description = string.Format(AppText.Scan_Corrupt_Package_Found_Description, modFile.Path),
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = modFile.Path,
            GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthCorruptScan{settings.Type}", UriKind.Absolute),
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = AppText.Scan_Corrupt_Found_Move_Label,
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
                    CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
            Caption = AppText.Scan_Corrupt_Package_NoneFound_Caption,
            Description = AppText.Scan_Corrupt_Package_NoneFound_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateUncomfortableScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantRemove,
            Caption = string.Format(AppText.Scan_Corrupt_Found_Caption, file.Name),
            Description = string.Format(AppText.Scan_Corrupt_Package_FoundOutOfRange_Description, modFile.Path),
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = modFile.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = AppText.Scan_Corrupt_Found_Move_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = "moveToDownloads"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_Corrupt_Found_StopTellingMe_CautionText,
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForCorruptMods = false;
}
