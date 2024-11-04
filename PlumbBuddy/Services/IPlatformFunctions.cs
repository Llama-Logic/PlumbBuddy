namespace PlumbBuddy.Services;

public interface IPlatformFunctions
{
    IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> DiscardableFileNamePatterns { get; }
    StringComparison FileSystemStringComparison { get; }
    IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> ForeignFileNamePatterns { get; }

    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    Task SendLocalNotificationAsync(string caption, string text);
    Task SetBadgeNumberAsync(int number);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
