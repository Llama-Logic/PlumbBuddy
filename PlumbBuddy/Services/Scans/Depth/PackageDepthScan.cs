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
            Caption = "No Packages are Too Deep",
            Description = "I didn't find any package files more than five folders deep in your Mods folder. That's good because if I did, I can assure you the game would not have.",
            Origin = this,
            Type = ScanIssueType.Healthy
        };

    protected override ScanIssue GenerateSickScanIssue(FileInfo file, ModFile modFile) =>
        new()
        {
            Icon = MaterialDesignIcons.Normal.FolderArrowUpDown,
            Caption = $"A Mod File is Too Deep: {file.Name}",
            Description =
                $"""
                I found this package file too deep in your Mods folder, specifically at this path:
                `{modFile.Path}`<br />
                We need to move it closer to the root of your Mods folder so the game can find it.
                """,
            Origin = this,
            Type = ScanIssueType.Sick,
            Data = modFile.Path,
            Resolutions =
            [
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FolderMove,
                    Label = "Move it closer to the root of my Mods folder",
                    Color = MudBlazor.Color.Primary,
                    Data = "move"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.FileFind,
                    Label = "Show me this package file",
                    Color = MudBlazor.Color.Secondary,
                    Data = "show"
                },
                new()
                {
                    Icon = MaterialDesignIcons.Normal.Cancel,
                    Label = "Stop telling me",
                    CautionCaption = "Disable this scan?",
                    CautionText = "I understand that this might be annoying, but the game has its limitations. And turning this scan off doesn't change that. All it will do is hide this warning.",
                    Data = "stopTellingMe"
                }
            ]
        };

    protected override void StopScanning(ISettings settings) =>
        settings.ScanForInvalidModSubdirectoryDepth = false;
}
