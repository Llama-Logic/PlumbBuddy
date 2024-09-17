namespace PlumbBuddy.Platforms.MacCatalyst;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public async Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        using var bash = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"find '{installationDirectory.FullName}' -type f -perm +111 -exec basename {{}} \\; | xargs pgrep -f\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        using var reader = bash!.StandardOutput;
        return (await reader.ReadToEndAsync().ConfigureAwait(false))
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var pid) ? pid : (int?)null)
            .Where(pid => pid.HasValue)
            .Select(pid => pid!.Value)
            .FirstOrDefault() is { } processId
            ? Process.GetProcessById(processId)
            : null;
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", $"\"{directoryInfo.FullName}\"");

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");
}
