namespace PlumbBuddy.App.Platforms.Windows;

class PlatformFunctions :
    IPlatformFunctions
{
    public StringComparison FileSystemStringComparison =>
        StringComparison.OrdinalIgnoreCase;

    public void ViewDirectory(DirectoryInfo directoryInfo) =>
        Process.Start("explorer.exe", directoryInfo.FullName);

    public void ViewFile(FileInfo fileInfo) =>
        Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
}
