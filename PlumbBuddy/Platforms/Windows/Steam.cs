using Microsoft.Win32;

namespace PlumbBuddy.Platforms.Windows;

class Steam :
    Services.SteamBase
{
    const string steamSteamPathValueName = "SteamPath";
    const string steamSubKeyName = @"Software\Valve\Steam";

    protected override DirectoryInfo? GetSteamDataDirectory()
    {
        try
        {
            if (Registry.CurrentUser.OpenSubKey(steamSubKeyName) is not { } steamSubKey)
                return null;
            var kind = steamSubKey.GetValueKind(steamSteamPathValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return null;
            if (steamSubKey.GetValue(steamSteamPathValueName) is not string path)
                return null;
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists)
                return directoryInfo;
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    protected override FileSystemInfo GetTS4Executable(DirectoryInfo installationDirectory) =>
        new FileInfo(Path.Combine(installationDirectory.FullName, "Game", "Bin", "TS4_x64.exe"));

    public override async Task<Version?> GetTS4InstallationVersionAsync()
    {
        if (await GetTS4InstallationDirectoryAsync().ConfigureAwait(false) is not { } installationDirectory
            || !installationDirectory.Exists)
            return null;
        if (GetTS4Executable(installationDirectory) is not FileInfo executable ||
            !executable.Exists)
            return null;
        var versionInfo = FileVersionInfo.GetVersionInfo(executable.FullName);
        if (versionInfo.FileVersion is { } fileVersion
            && !string.IsNullOrWhiteSpace(fileVersion)
            && Version.TryParse(fileVersion, out var version))
            return version;
        return null;
    }
}
