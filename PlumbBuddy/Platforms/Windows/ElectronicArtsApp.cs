using Microsoft.Win32;
using Windows.Win32;

namespace PlumbBuddy.Platforms.Windows;

class ElectronicArtsApp(ILogger<IElectronicArtsApp> logger) :
    Services.ElectronicArtsApp(logger)
{
    static bool GetEaDesktopAppExecutableBinaryFile([NotNullWhen(true)] out FileInfo? eaDesktopAppExecutableBinaryFile)
    {
        eaDesktopAppExecutableBinaryFile = default;
        try
        {
            if (Registry.LocalMachine.OpenSubKey(eaAppSubKeyName) is not { } eaAppSubKey)
                return false;
            var kind = eaAppSubKey.GetValueKind(eaAppDesktopAppPathValueName);
            if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                return false;
            if (eaAppSubKey.GetValue(eaAppDesktopAppPathValueName) is not string path)
                return false;
            if (kind is RegistryValueKind.ExpandString)
                path = Environment.ExpandEnvironmentVariables(path);
            eaDesktopAppExecutableBinaryFile = new(path);
            if (!eaDesktopAppExecutableBinaryFile.Exists)
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

    const string eaAppDesktopAppPathValueName = "DesktopAppPath";
    const string eaAppInstallLocationValueName = "InstallLocation";
    const string eaAppSubKeyName = @"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop";
    const string ts4InstallDirValueName = "Install Dir";
    const string ts4SubKeyName = @"SOFTWARE\WOW6432Node\Maxis\The Sims 4";

    public override async Task<DirectoryInfo?> GetElectronicArtsUserDataDirectoryAsync()
    {
        if (!await GetIsElectronicArtsAppInstalledAsync().ConfigureAwait(false))
            return null;
        var userDataDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Electronic Arts", "EA Desktop"));
        if (!userDataDirectory.Exists)
            return null;
        return userDataDirectory;
    }

    public override Task<bool> GetIsElectronicArtsAppInstalledAsync()
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

    public override Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
    {
        if (GetTS4InstallationExecutableBinary() is not { } executable)
            return Task.FromResult<DirectoryInfo?>(null);
        if (executable.Exists)
            return Task.FromResult<DirectoryInfo?>(executable.Directory!.Parent!.Parent!);
        return Task.FromResult<DirectoryInfo?>(null);
    }

    public override Task LaunchElectronicArtsAppAsync()
    {
        if (!GetEaDesktopAppExecutableBinaryFile(out var eaDesktopAppExecutableBinaryFile))
            return Task.CompletedTask;
        Process.Start(new ProcessStartInfo
        {
            FileName = eaDesktopAppExecutableBinaryFile.FullName
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task<bool> QuitElectronicArtsAppAsync()
    {
        if (!GetEaDesktopAppExecutableBinaryFile(out var eaDesktopAppExecutableBinaryFile))
            return false;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainModule is not { } mainModule
                    || Path.GetFullPath(mainModule.FileName) != Path.GetFullPath(eaDesktopAppExecutableBinaryFile.FullName))
                    continue;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            catch (Win32Exception)
            {
                continue;
            }
            process.CloseMainWindow();
            PInvoke.PostThreadMessage((uint)process.Id, PInvoke.WM_QUIT, 0, 0);
            await process.WaitForExitAsync().ConfigureAwait(false);
            return true;
        }
        return false;
    }
}
