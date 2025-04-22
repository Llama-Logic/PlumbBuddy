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

    FileInfo? GetTS4InstallationExecutableBinary()
    {
        try
        {
            if (Registry.LocalMachine.OpenSubKey(ts4SubKeyName) is not { } ts4SubKey)
                return null;
            var kind = ts4SubKey.GetValueKind(ts4InstallDirValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return null;
            if (ts4SubKey.GetValue(ts4InstallDirValueName) is not string path)
                return null;
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
                return null;
            return new FileInfo(Path.Combine(directoryInfo.FullName, "Game", "Bin", "TS4_x64.exe"));
        }
        catch (IOException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
    {
        if (GetTS4InstallationExecutableBinary() is not { } executable)
            return Task.FromResult<DirectoryInfo?>(null);
        if (executable.Exists)
            return Task.FromResult<DirectoryInfo?>(executable.Directory!.Parent!.Parent!);
        return Task.FromResult<DirectoryInfo?>(null);
    }
}
