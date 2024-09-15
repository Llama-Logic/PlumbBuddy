using Microsoft.Win32;

namespace PlumbBuddy.Platforms.Windows;

class ElectronicArtsApp :
    IElectronicArtsApp
{
    const string eaAppInstallLocationValueName = "InstallLocation";
    const string eaAppSubKeyName = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
    const string ts4InstallDirValueName = "Install Dir";
    const string ts4SubKeyName = @"SOFTWARE\WOW6432Node\Maxis\The Sims 4";

    public Task<bool> GetIsElectronicArtsAppInstalledAsync()
    {
        try
        {
            if (Registry.LocalMachine.OpenSubKey(eaAppSubKeyName) is not { } eaAppSubKey)
                return Task.FromResult(false);
            var kind = eaAppSubKey.GetValueKind(eaAppInstallLocationValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return Task.FromResult(false);
            if (eaAppSubKey.GetValue(eaAppInstallLocationValueName) is not string path)
                return Task.FromResult(false);
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            if (Directory.Exists(path))
                return Task.FromResult(true);
            return Task.FromResult(false);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
        catch (SecurityException)
        {
            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
    {
        try
        {
            if (Registry.LocalMachine.OpenSubKey(ts4SubKeyName) is not { } ts4SubKey)
                return Task.FromResult<DirectoryInfo?>(null);
            var kind = ts4SubKey.GetValueKind(ts4InstallDirValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return Task.FromResult<DirectoryInfo?>(null);
            if (ts4SubKey.GetValue(ts4InstallDirValueName) is not string path)
                return Task.FromResult<DirectoryInfo?>(null);
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                return Task.FromResult<DirectoryInfo?>(null);
            if (File.Exists(Path.Combine(directoryInfo.FullName, "Game", "Bin", "TS4_x64.exe")))
                return Task.FromResult<DirectoryInfo?>(directoryInfo);
            return Task.FromResult<DirectoryInfo?>(null);
        }
        catch (IOException)
        {
            return Task.FromResult<DirectoryInfo?>(null);
        }
        catch (SecurityException)
        {
            return Task.FromResult<DirectoryInfo?>(null);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<DirectoryInfo?>(null);
        }
    }
}
