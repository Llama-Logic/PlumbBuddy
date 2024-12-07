namespace PlumbBuddy.Services.Scans.Depth;

public sealed class PackageDepthScan :
    DepthScan,
    IPackageDepthScan
{
    public PackageDepthScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.Package, 5)
    {
    }

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderArrowUpDown,
            Caption = AppText.Scan_Depth_Package_NoneFound_Caption,
            Description = AppText.Scan_Depth_Package_NoneFound_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateSickScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderArrowUpDown,
            Caption = string.Format(AppText.Scan_Depth_TooDeep_Caption, file.Name),
            Description = string.Format(AppText.Scan_Depth_Package_TooDeep_Description, modFile.Path),
            Origin = this,
            Type = ScanIssueType.Sick,
            Data = modFile.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = AppText.Scan_Depth_TooDeep_MoveCloserToModsRoot_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = "move"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FileFind,
                    Label = AppText.Scan_Depth_Package_TooDeep_ShowMe_Label,
                    Color = MudBlazor.Color.Secondary,
                    Data = "show"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_Depth_TooDeep_StopTellingMe_CautionCaption,
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForInvalidModSubdirectoryDepth = false;
}
