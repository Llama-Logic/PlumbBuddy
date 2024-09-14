using Microsoft.Win32;

namespace PlumbBuddy.App.Platforms.Windows;

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
}
