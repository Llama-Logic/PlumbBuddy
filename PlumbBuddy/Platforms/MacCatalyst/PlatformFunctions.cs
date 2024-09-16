namespace PlumbBuddy.Platforms.MacCatalyst;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.Ordinal;

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("open", $"\"{directoryInfo.FullName}\"");

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("open", $"-R \"{fileInfo.FullName}\"");
}
