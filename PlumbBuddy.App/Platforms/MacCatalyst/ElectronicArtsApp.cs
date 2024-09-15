namespace PlumbBuddy.App.Platforms.MacCatalyst;

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
                    Arguments = $"-onlyin /Applications \"kMDItemCFBundleIdentifier == '{bundleId}'\"",
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
}