namespace PlumbBuddy.Services.Scans.Setting;

public sealed class ScriptModSettingScan :
    SettingScan,
    IScriptModSettingScan
{
    const string deadScanIssueData = "scriptArchivesFoundButScriptModsDisabled";
    const string deadScanIssueFixResolutionData = "enableScriptMods";
    const string deadScanIssueStopResolutionData = "stopScanning";

    public ScriptModSettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, IPlayer player, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, player, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.ScriptArchive, deadScanIssueData, deadScanIssueFixResolutionData, deadScanIssueStopResolutionData)
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
            Caption = "Script Mods are Disabled in Options",
            Description = "You have script archives installed, but either the **Enable Custom Content and Mods** box or the **Script Mods Allowed** box (or both) is unchecked in **Game Options**.",
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = deadScanIssueData,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.AutoFix,
                    Label = "Check the boxes for me",
                    Color = MudBlazor.Color.Primary,
                    Data = deadScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "Your script mods will not work at all without these Game Options enabled, whether or not I am running this scan. By telling me not to, you're just getting rid of this warning, not fixing the problem.",
                    Data = deadScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.SourceBranchCheck,
            Caption = "Script Mods Game Option is Fine",
            Description = "The **Script Mods Allowed** box in **Game Options** isn't causing any problems at all at the moment.",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(IPlayer player) =>
        player.ScanForScriptModsDisabled = false;
}
