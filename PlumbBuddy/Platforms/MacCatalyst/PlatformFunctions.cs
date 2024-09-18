namespace PlumbBuddy.Platforms.MacCatalyst;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public async Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        var gameProcess = Process.GetProcesses().Where(process => process.ProcessName.Contains("sims", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        return gameProcess;
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", $"\"{directoryInfo.FullName}\"");

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");
}
