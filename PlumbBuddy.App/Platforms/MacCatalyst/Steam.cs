namespace PlumbBuddy.App.Platforms.MacCatalyst;

class Steam :
    Services.SteamBase
{
    protected override DirectoryInfo? GetSteamDataDirectory()
    {
        var steamDataDirectory = new DirectoryInfo(Path.Combine
        (
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Library",
            "Application Support",
            "Steam"
        ));
        return steamDataDirectory.Exists
            ? steamDataDirectory
            : null;
    }

    protected override FileSystemInfo GetTS4Executable(DirectoryInfo installationDirectory) =>
        new DirectoryInfo(Path.Combine(installationDirectory.FullName, "The Sims 4.app"));
}
