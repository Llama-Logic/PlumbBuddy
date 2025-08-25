namespace PlumbBuddy.Platforms.MacCatalyst;

class ElectronicArtsApp(ILogger<IElectronicArtsApp> logger) :
    Services.ElectronicArtsApp(logger)
{
    const string eaAppBundleId = "com.ea.mac.eaapp";
    const string ts4AppBundleId = "com.ea.mac.thesims4";

    async Task<string?> FindAppByBundleIdAsync(string bundleId)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/mdfind",
                    Arguments = $"\"kMDItemCFBundleIdentifier == '{bundleId}'\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);
            if (process.ExitCode is not 0)
                return null;
            return output;
        }
        catch
        {
            return null;
        }
    }

    public override async Task<DirectoryInfo?> GetElectronicArtsUserDataDirectoryAsync()
    {
        if (!await GetIsElectronicArtsAppInstalledAsync().ConfigureAwait(false))
            return null;
        var userDataDirectory = new DirectoryInfo(Path.Combine(FileSystem.AppDataDirectory, "Application Support", "Electronic Arts", "EA app"));
        if (!userDataDirectory.Exists)
            return null;
        return userDataDirectory;
    }

    public override async Task<bool> GetIsElectronicArtsAppInstalledAsync()
    {
        var eaAppPath = await FindAppByBundleIdAsync(eaAppBundleId);
        return !string.IsNullOrEmpty(eaAppPath);
    }

    async Task<bool> GetIsElectronicArtsAppRunningAsync()
    {
        using var osascriptProcess = Process.Start(new ProcessStartInfo("/usr/bin/osascript", $"-e 'tell application id \"{eaAppBundleId}\" to return (running as integer)'")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        });
        if (osascriptProcess is null)
            return false;
        var output = await osascriptProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await osascriptProcess.WaitForExitAsync().ConfigureAwait(false);
        return osascriptProcess.ExitCode is 0 && output.Trim() == "1";
    }

    public override async Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
    {
        var ts4AppPath = await FindAppByBundleIdAsync(ts4AppBundleId).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(ts4AppPath))
        {
            var directoryInfo = new DirectoryInfo(ts4AppPath);
            if (directoryInfo.Exists)
                return directoryInfo;
        }
        return null;
    }

    public override async Task LaunchElectronicArtsAppAsync()
    {
        using var openProcess = Process.Start(new ProcessStartInfo("/usr/bin/open", $"-b {eaAppBundleId}")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (openProcess is null)
            return;
        await openProcess.WaitForExitAsync().ConfigureAwait(false);
        return;
    }

    public override async Task<bool> QuitElectronicArtsAppAsync()
    {
        if (!await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false))
            return false;
        using var osascriptProcess = Process.Start(new ProcessStartInfo("/usr/bin/osascript", $"-e 'tell application id \"{eaAppBundleId}\" to quit'")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (osascriptProcess is null)
            return false;
        await osascriptProcess.WaitForExitAsync().ConfigureAwait(false);
        while (await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false))
            await Task.Delay(500).ConfigureAwait(false);
        return true;
    }
}