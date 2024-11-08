namespace PlumbBuddy.Platforms.MacCatalyst;

class ElectronicArtsApp :
    IElectronicArtsApp
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

    public async Task<bool> GetIsElectronicArtsAppInstalledAsync()
    {
        var eaAppPath = await FindAppByBundleIdAsync(eaAppBundleId);
        return !string.IsNullOrEmpty(eaAppPath);
    }

    public async Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
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

    public async Task<Version?> GetTS4InstallationVersionAsync()
    {
        if (await GetTS4InstallationDirectoryAsync().ConfigureAwait(false) is not { } appBundle ||
            !appBundle.Exists)
            return null;
        var defaultIni = new FileInfo(Path.Combine(appBundle.FullName, "Contents", "Resources", "Default.ini"));
        if (!defaultIni.Exists)
            return null;
        var parser = new IniDataParser();
        var data = parser.Parse(await File.ReadAllTextAsync(defaultIni.FullName).ConfigureAwait(false));
        var versionData = data["Version"];
        if (versionData["gameversion"] is { } gameVersion
            && !string.IsNullOrWhiteSpace(gameVersion)
            && Version.TryParse(gameVersion, out var version))
            return version;
        return null;
    }
}