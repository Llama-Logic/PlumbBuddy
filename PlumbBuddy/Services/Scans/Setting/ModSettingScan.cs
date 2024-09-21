namespace PlumbBuddy.Services.Scans.Setting;

public sealed class ModSettingScan :
    SettingScan,
    IModSettingScan
{
    const string deadScanIssueData = "packagesFoundButModsDisabled";
    const string deadScanIssueFixResolutionData = "enableMods";
    const string deadScanIssueStopResolutionData = "stopScanning";

    public ModSettingScan(PbDbContext pbDbContext, IPlayer player, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContext, player, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.Package, deadScanIssueData, deadScanIssueFixResolutionData, deadScanIssueStopResolutionData)
    {
    }

    protected override bool AreGameOptionsDisablingFeature(ISmartSimObserver smartSimObserver) =>
        smartSimObserver.IsModsDisabledGameSettingOn;

    protected override void CorrectIniOptions(IniParser.Model.KeyDataCollection options) =>
        options["modsdisabled"] = "0";

    protected override ScanIssue GenerateDeadScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantClosedRemove,
            Caption = "Mods are Disabled in Options",
            Description = "You have mod packages installed, but the **Enable Custom Content and Mods** box is unchecked in **Game Options**.",
            Origin = this,
            Type = ScanIssueType.Dead,
            Data = deadScanIssueData,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.CheckboxMarked,
                    Label = "Check the box for me",
                    Color = MudBlazor.Color.Primary,
                    Data = deadScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "Your custom content and mods will not work at all without this Game Option enabled, whether or not I am running this scan. By telling me not to, you're just getting rid of this warning, not fixing the problem.",
                    Data = deadScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.PackageVariantClosedCheck,
            Caption = "Mods Game Option is Fine",
            Description = "The **Enable Custom Content and Mods** box in **Game Options** isn't causing any problems at all at the moment.",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(IPlayer player) =>
        player.ScanForModsDisabled = false;
}
