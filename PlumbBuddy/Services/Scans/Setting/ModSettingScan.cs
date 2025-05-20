namespace PlumbBuddy.Services.Scans.Setting;

public sealed class ModSettingScan :
    SettingScan,
    IModSettingScan
{
    const string deadScanIssueData = "packagesFoundButModsDisabled";
    const string deadScanIssueFixResolutionData = "enableMods";
    const string deadScanIssueStopResolutionData = "stopScanning";

    public ModSettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, settings, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.Package, deadScanIssueData, deadScanIssueFixResolutionData, deadScanIssueStopResolutionData)
    {
    }

    protected override bool AreGameOptionsUndesirable(ISmartSimObserver smartSimObserver) =>
        smartSimObserver.IsModsDisabledGameSettingOn;

    protected override void CorrectIniOptions(IniParser.Model.KeyDataCollection options) =>
        options["modsdisabled"] = "0";

    protected override ScanIssue GenerateUndesirableScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantClosedRemove,
            Caption = AppText.Scan_Setting_ModsGameOption_Incorrect_Caption,
            Description = AppText.Scan_Setting_ModsGameOption_Incorrect_Description,
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = deadScanIssueData,
            GuideUrl = new($"https://plumbbuddy.app/redirect?to=PlumbBuddyInAppGuideModHealthModSettingScan{settings.Type}", UriKind.Absolute),
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.AutoFix,
                    Label = AppText.Scan_Setting_ModsGameOption_Correct_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = deadScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_Setting_ModsGameOption_StopTellingMe_CautionText,
                    Data = deadScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
            Caption = AppText.Scan_Setting_ModsGameOption_Okay_Caption,
            Description = AppText.Scan_Setting_ModsGameOption_Okay_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForModsDisabled = false;
}
