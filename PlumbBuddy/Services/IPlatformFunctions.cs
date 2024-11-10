namespace PlumbBuddy.Services;

public interface IPlatformFunctions
{
    IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> DiscardableFileNamePatterns { get; }
    StringComparison FileSystemStringComparison { get; }
    IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> ForeignFileNamePatterns { get; }

    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    Task<bool> SendLocalNotificationAsync(string caption, string text);
    Task<bool> SetBadgeNumberAsync(int number);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
