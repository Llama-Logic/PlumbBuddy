namespace PlumbBuddy.Services.Scans.Setting;

public class ShowModListStartupSettingScan :
    SettingScan,
    IShowModListStartupSettingScan
{
    const string uncomfortableScanIssueData = "packagesFoundButShowAtStartup";
    const string uncomfortableScanIssueFixResolutionData = "disableShowAtStartup";
    const string uncomfortableScanIssueStopResolutionData = "stopScanning";

    public ShowModListStartupSettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, settings, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.Package, uncomfortableScanIssueData, uncomfortableScanIssueFixResolutionData, uncomfortableScanIssueStopResolutionData)
    {
    }

    protected override bool AreGameOptionsUndesirable(ISmartSimObserver smartSimObserver)
    {
        ArgumentNullException.ThrowIfNull(smartSimObserver);
        return smartSimObserver.IsShowModListStartupGameSettingOn;
    }

    protected override void CorrectIniOptions(IniParser.Model.KeyDataCollection options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options["showmodliststartup"] = "0";
    }

    protected override ScanIssue GenerateUndesirableScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SpeedometerSlow,
            Caption = AppText.Scan_Setting_ShowModsListGameOption_Incorrect_Caption,
            Description = AppText.Scan_Setting_ShowModsListGameOption_Incorrect_Description,
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = uncomfortableScanIssueData,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.AutoFix,
                    Label = AppText.Scan_Setting_ShowModsListGameOption_Correct_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = uncomfortableScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_Setting_ShowModsListGameOption_StopTellingMe_CautionText,
                    Data = uncomfortableScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.Speedometer,
            Caption = AppText.Scan_Setting_ShowModsListGameOption_Okay_Caption,
            Description = AppText.Scan_Setting_ShowModsListGameOption_Okay_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.ScanForShowModsListAtStartupEnabled = false;
    }
}
