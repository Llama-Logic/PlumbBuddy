namespace PlumbBuddy.Services;

public interface IPlatformFunctions
{
    StringComparison FileSystemStringComparison { get; }

    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
