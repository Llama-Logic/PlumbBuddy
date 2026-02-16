namespace PlumbBuddy.Platforms.MacCatalyst;

class ElectronicArtsApp(ILogger<IElectronicArtsApp> logger) :
    Services.ElectronicArtsApp(logger)
{
    const string eaAppBundleId = "com.ea.mac.eaapp";
    const string ts4AppBundleId = "com.ea.mac.thesims4";

    static async Task<string> GetUidAsync()
    {
        using var p = Process.Start(new ProcessStartInfo("/usr/bin/id")
        {
            ArgumentList = { "-u" },
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        if (p is null)
            return "501";
        var output = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await p.WaitForExitAsync().ConfigureAwait(false);
        var uid = output.Trim();
        return string.IsNullOrWhiteSpace(uid)
            ? "501"
            : uid;
    }

    async Task<string?> FindAppByBundleIdAsync(string bundleId)
    {
        string? bundlePath = null;
        try
        {
            using var mdfindProcess = new Process
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
            mdfindProcess.Start();
            var mdfindOutput = (await mdfindProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false))?.Trim();
            await mdfindProcess.WaitForExitAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(mdfindOutput)
                && mdfindProcess.ExitCode is 0
                && bundleId.Equals(await ReadBundleIdAsync(new(mdfindOutput)).ConfigureAwait(false), StringComparison.OrdinalIgnoreCase))
                bundlePath = mdfindOutput;
        }
        catch
        {
            // whoops, mdfind didn't work
        }
        if (bundlePath is null)
            foreach (DirectoryInfo root in GetCandidateRoots())
                foreach (var bundle in root.GetDirectories("*.app", SearchOption.AllDirectories))
                    if (await ReadBundleIdAsync(bundle).ConfigureAwait(false) is { } appBundleId
                        && bundleId.Equals(appBundleId, StringComparison.OrdinalIgnoreCase))
                        return bundle.FullName;
        return bundlePath;
    }

    static IEnumerable<DirectoryInfo> GetCandidateRoots()
    {
        var apps = new DirectoryInfo("/Applications");
        if (apps.Exists)
            yield return apps;
        var userApps = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications"));
        if (userApps.Exists)
            yield return userApps;
        var volumes = new DirectoryInfo("/Volumes");
        if (volumes.Exists)
        {
            try
            {
                foreach (var volume in volumes.GetDirectories())
                {
                    var volumeApps = new DirectoryInfo(Path.Combine(volume.FullName, "Applications"));
                    if (volumeApps.Exists)
                        yield return volumeApps;
                }
            }
            catch
            {
                // permission denied or otherwise inaccessible to a user process, don't cry over it
            }
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
        var psi = new ProcessStartInfo("/usr/bin/osascript")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add($"tell application id \"{eaAppBundleId}\" to return (running as integer)");
        using var osascriptProcess = Process.Start(psi);
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
        var psi = new ProcessStartInfo("/usr/bin/osascript")
        {
            CreateNoWindow = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add($"tell application id \"{eaAppBundleId}\" to quit");
        using var osascriptProcess = Process.Start(psi);
        if (osascriptProcess is null)
            return false;
        var patience = 100;
        var osascriptProcessWaitForExit = osascriptProcess.WaitForExitAsync();
        while (!osascriptProcess.HasExited && --patience > 0)
            await Task.WhenAny(osascriptProcessWaitForExit, Task.Delay(TimeSpan.FromSeconds(0.1))).ConfigureAwait(false);
        if (!await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false))
            return true;
        var uid = await GetUidAsync().ConfigureAwait(false);
        var terminateProcess = Process.Start(new ProcessStartInfo("/bin/launchctl")
        {
            Arguments = $"kill TERM gui/{uid}/{eaAppBundleId}",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        if (terminateProcess is null)
            return false;
        await terminateProcess.WaitForExitAsync().ConfigureAwait(false);
        patience = 50;
        while (await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false) && --patience > 0)
            await Task.Delay(TimeSpan.FromSeconds(0.1)).ConfigureAwait(false);
        if (!await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false))
            return true;
        var killProcess = Process.Start(new ProcessStartInfo("/bin/launchctl")
        {
            Arguments = $"kill KILL gui/{uid}/{eaAppBundleId}",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        if (killProcess is null)
            return false;
        await killProcess.WaitForExitAsync().ConfigureAwait(false);
        patience = 50;
        while (await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false) && --patience > 0)
            await Task.Delay(TimeSpan.FromSeconds(0.1)).ConfigureAwait(false);
        return !await GetIsElectronicArtsAppRunningAsync().ConfigureAwait(false);
    }

    static async Task<string?> ReadBundleIdAsync(DirectoryInfo appDir)
    {
        if (!appDir.Exists)
            return null;
        var infoPlistFile = new FileInfo(Path.Combine(appDir.FullName, "Contents", "Info.plist"));
        if (!infoPlistFile.Exists)
            return null;
        var startInfo = new ProcessStartInfo
        {
            FileName = "/usr/libexec/PlistBuddy",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Print :CFBundleIdentifier");
        startInfo.ArgumentList.Add(infoPlistFile.FullName);
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode == 0 ? output.Trim() : null;
    }
}