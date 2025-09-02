namespace PlumbBuddy.Platforms.MacCatalyst;

class Steam :
    SteamBase
{
    public override Task<bool> GetIsSteamRunningAsync() =>
        throw new NotImplementedException();

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

    public override Task<DirectoryInfo?> GetSteamUserDataDirectoryAsync() =>
        throw new NotImplementedException();

    protected override FileSystemInfo GetTS4Executable(DirectoryInfo installationDirectory) =>
        new DirectoryInfo(Path.Combine(installationDirectory.FullName, "The Sims 4.app"));

    override Task LaunchSteamAsync() =>
        throw new NotImplementedException();

    override Task QuitSteamAsync() =>
        throw new NotImplementedException();
}
