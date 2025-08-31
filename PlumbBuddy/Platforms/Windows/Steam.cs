using Microsoft.Win32;

namespace PlumbBuddy.Platforms.Windows;

class Steam :
    Services.SteamBase
{
    const string steamSteamExeValueName = "SteamExe";
    const string steamSteamPathValueName = "SteamPath";
    const string steamSubKeyName = @"Software\Valve\Steam";

    public override Task<bool> GetIsSteamRunningAsync() =>
        Task.FromResult(Process.GetProcessesByName("steam") is not null);

    static bool GetSteamExecutableBinaryFile([NotNullWhen(true)] out FileInfo? steamExecutableBinaryFile)
    {
        steamExecutableBinaryFile = default;
        try
        {
            if (Registry.CurrentUser.OpenSubKey(steamSubKeyName) is not { } steamSubKey)
                return false;
            var kind = steamSubKey.GetValueKind(steamSteamExeValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return false;
            if (steamSubKey.GetValue(steamSteamExeValueName) is not string path)
                return false;
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            steamExecutableBinaryFile = new(path);
            if (!steamExecutableBinaryFile.Exists)
                return false;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

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

    public override Task LaunchSteamAsync()
    {
        if (!GetSteamExecutableBinaryFile(out var steamExecutableBinaryFile))
            return Task.CompletedTask;
        Process.Start(new ProcessStartInfo
        {
            FileName = steamExecutableBinaryFile.FullName
        });
        return Task.CompletedTask;
    }

    public override async Task<bool> QuitSteamAsync()
    {
        if (!GetSteamExecutableBinaryFile(out var steamExecutableBinaryFile)
            || !await GetIsSteamRunningAsync().ConfigureAwait(false))
            return false;
        using var shutdownProcess = Process.Start(new ProcessStartInfo
        {
            FileName = steamExecutableBinaryFile.FullName,
            Arguments = "-shutdown"
        });
        if (shutdownProcess is not null)
            await shutdownProcess.WaitForExitAsync().ConfigureAwait(false);
        return true;
    }
}
