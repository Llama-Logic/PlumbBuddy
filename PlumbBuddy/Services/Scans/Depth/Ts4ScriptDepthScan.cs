namespace PlumbBuddy.Services.Scans.Depth;

public sealed class Ts4ScriptDepthScan :
    DepthScan,
    ITs4ScriptDepthScan
{
    public Ts4ScriptDepthScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, platformFunctions, settings, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.ScriptArchive, 1)
    {
    }

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderArrowUpDown,
            Caption = AppText.Scan_Depth_Ts4Script_NoneFound_Caption,
            Description = AppText.Scan_Depth_Ts4Script_NoneFound_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateSickScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderArrowUpDown,
            Caption = string.Format(AppText.Scan_Depth_TooDeep_Caption, file.Name),
            Description = string.Format(AppText.Scan_Depth_Ts4Script_TooDeep_Description, modFile.Path),
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
                    Label = AppText.Scan_Depth_Ts4Script_TooDeep_ShowMe_Label,
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
        settings.ScanForInvalidScriptModSubdirectoryDepth = false;
}
