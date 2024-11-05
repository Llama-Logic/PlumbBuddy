namespace PlumbBuddy.Services.Scans.Setting;

public class ShowModListStartupSettingScan :
    SettingScan,
    IShowModListStartupSettingScan
{
    const string uncomfortableScanIssueData = "packagesFoundButShowAtStartup";
    const string uncomfortableScanIssueFixResolutionData = "disableShowAtStartup";
    const string uncomfortableScanIssueStopResolutionData = "stopScanning";

    public ShowModListStartupSettingScan(IDbContextFactory<PbDbContext> pbDbContextFactory, ISettings player, ISmartSimObserver smartSimObserver, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks) :
        base(pbDbContextFactory, player, smartSimObserver, modsDirectoryCataloger, superSnacks, ModsDirectoryFileType.Package, uncomfortableScanIssueData, uncomfortableScanIssueFixResolutionData, uncomfortableScanIssueStopResolutionData)
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
            Caption = "Show Mod List is Enabled in Options",
            Description = "You have mod packages installed and the **Show At Startup** box inside the **View Custom Content** button in **Game Options** is checked. This can make your game load very slowly, especially on older computers or those with hard disk drives.",
            Origin = this,
            Type = ScanIssueType.Uncomfortable,
            Data = uncomfortableScanIssueData,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.AutoFix,
                    Label = "Uncheck the box for me",
                    Color = MudBlazor.Color.Primary,
                    Data = uncomfortableScanIssueFixResolutionData
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "You can just open your mods folder and look at the mods you have with the convenient button right on the top of my window, so you really don't need the game doing this. Disabling this scan won't cause your game to load any faster.",
                    Data = uncomfortableScanIssueStopResolutionData
                }
            ]
        };

    protected override ScanIssue GenerateHealthyScanIssue() =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.Speedometer,
            Caption = "Show Mod List Option is Fine",
            Description = "Either you have no mods or the **Show At Startup** box inside the **View Custom Content** button in **Game Options** is not checked, which means your game isn't loading slowly just to open that list when you launch it. That's great. Any time you want to see your mods, just click the convenient button on the top of my window to open your Mods folder right up.",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override void StopScanning(ISettings player) =>
        player.ScanForShowModsListAtStartupEnabled = false;
}
