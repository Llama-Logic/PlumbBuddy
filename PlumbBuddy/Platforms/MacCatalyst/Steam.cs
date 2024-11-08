namespace PlumbBuddy.Platforms.MacCatalyst;

class Steam :
    SteamBase
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

    public override async Task<Version?> GetTS4InstallationVersionAsync()
    {
        if (await GetTS4InstallationDirectoryAsync().ConfigureAwait(false) is not { } appBundle
            || !appBundle.Exists)
            return null;
        var defaultIni = new FileInfo(Path.Combine(appBundle.FullName, "Contents", "Resources", "Default.ini"));
        if (!defaultIni.Exists)
            return null;
        var parser = new IniDataParser();
        var data = parser.Parse(await File.ReadAllTextAsync(defaultIni.FullName).ConfigureAwait(false));
        var versionData = data["Version"];
        if (versionData["gameversion"] is { } gameVersion
            && string.IsNullOrWhiteSpace(gameVersion)
            && Version.TryParse(gameVersion, out var version))
            return version;
        return null;
    }
}
