namespace PlumbBuddy.Platforms.MacCatalyst;

class PlatformFunctions :
    IPlatformFunctions
{
    static readonly TimeSpan gameProcessScanGracePeriod = TimeSpan.FromSeconds(5);

    DateTimeOffset? lastGameProcessScan;

    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public async Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        if (lastGameProcessScan is { } last
            && last + gameProcessScanGracePeriod > DateTimeOffset.UtcNow)
            return null;
        lastGameProcessScan = DateTimeOffset.UtcNow;
        using var zshProcess = Process.Start(new ProcessStartInfo("/bin/zsh", $"-c \"pgrep -f 'The Sims 4'\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (zshProcess is not null)
        {
            using var zshOutputReader = zshProcess.StandardOutput;
            var zshOutput = await zshOutputReader.ReadToEndAsync().ConfigureAwait(false);
            var maybeNullProcessId = zshOutput
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => int.TryParse(line, out var processId) ? processId : (int?)null)
                .Where(maybeNullProcessId => maybeNullProcessId is not null)
                .FirstOrDefault();
            if (maybeNullProcessId is { } processId)
                return Process.GetProcessById(processId);
        }
        return null;
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", $"\"{directoryInfo.FullName}\"");

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");
}
