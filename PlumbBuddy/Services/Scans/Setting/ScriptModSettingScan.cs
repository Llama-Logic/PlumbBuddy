namespace PlumbBuddy.Services.Scans.Setting;

public sealed class ScriptModSettingScan :
    SettingScan,
    IScriptModSettingScan
{
    const string deadScanIssueData = "scriptArchivesFoundButScriptModsDisabled";
    const string deadScanIssueFixResolutionData = "enableScriptMods";
    const string deadScanIssueStopResolutionData = "stopScanning";

    public ScriptModSettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings settings, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, settings, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.ScriptArchive, deadScanIssueData, deadScanIssueFixResolutionData, deadScanIssueStopResolutionData)
    {
    }

    protected override bool AreGameOptionsUndesirable(ISmartSimObserver smartSimObserver) =>
        smartSimObserver.IsModsDisabledGameSettingOn || !smartSimObserver.IsScriptModsEnabledGameSettingOn;

    protected override void CorrectIniOptions(IniParser.Model.KeyDataCollection options)
    {
        options["modsdisabled"] = "0";
        options["scriptmodsenabled"] = "1";
    }

    protected override ScanIssue GenerateUndesirableScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchRemove,
            Caption = AppText.Scan_Setting_ScriptModsGameOption_Incorrect_Caption,
            Description = AppText.Scan_Setting_ScriptModsGameOption_Incorrect_Description,
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = deadScanIssueData,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.AutoFix,
                    Label = AppText.Scan_Setting_ScriptModsGameOption_Correct_Label,
                    Color = MudBlazor.Color.Primary,
                    Data = deadScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = AppText.Scan_Common_StopTellingMe_Label,
                    CautionCaption = AppText.Scan_Common_StopTellingMe_CautionCaption,
                    CautionText = AppText.Scan_Setting_ScriptModsGameOption_StopTellingMe_CautionText,
                    Data = deadScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchCheck,
            Caption = AppText.Scan_Setting_ScriptModsGameOption_Okay_Caption,
            Description = AppText.Scan_Setting_ScriptModsGameOption_Okay_Description,
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForScriptModsDisabled = false;
}
