namespace PlumbBuddy.Platforms.Windows;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.OrdinalIgnoreCase;

    public Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory)
    {
        ArgumentNullException.ThrowIfNull(installationDirectory);
        var gameProcess = Process.GetProcessesByName("TS4_x64").FirstOrDefault()
            ?? Process.GetProcessesByName("TS4_DX9_x64").FirstOrDefault();
        if (gameProcess is null
            || gameProcess.MainModule is not { } mainModule
            || !mainModule.FileName.StartsWith(installationDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Process?>(null);
        return Task.FromResult<Process?>(gameProcess);
    }

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("explorer.exe", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
}
