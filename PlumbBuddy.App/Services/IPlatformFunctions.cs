namespace PlumbBuddy.App.Services;

public interface IPlatformFunctions
{
    StringComparison FileSystemStringComparison { get; }

    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
