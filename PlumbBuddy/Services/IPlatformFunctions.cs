namespace PlumbBuddy.Services;

public interface IPlatformFunctions :
    INotifyPropertyChanged
{
    IReadOnlyList<Regex> DiscardableDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> DiscardableFileNamePatterns { get; }
    string FileSystemSQliteCollation { get; }
    StringComparison FileSystemStringComparison { get; }
    IReadOnlyList<Regex> ForeignDirectoryNamePatterns { get; }
    IReadOnlyList<Regex> ForeignFileNamePatterns { get; }
    string HostOperatingSystemUserAgentName { get; }

    string UserAgent
    {
        get
        {
            var mauiVersion = AppInfo.Version;
            var llpVersion = typeof(DataBasePackedFile).Assembly.GetName().Version;
            return $"PlumbBuddy/{mauiVersion.Major}.{mauiVersion.Minor}.{mauiVersion.Build} ({HostOperatingSystemUserAgentName} {DeviceInfo.VersionString}; {RuntimeInformation.ProcessArchitecture}) NETCLR/{RuntimeInformation.FrameworkDescription.Split(' ')[1]} (.NET Core CLR; MAUI Blazor Hybrid){(llpVersion is null ? string.Empty : $" LLP/{llpVersion.Major}.{llpVersion.Minor}.{llpVersion.Build}")}";
        }
    }

    nint DefaultProcessorAffinity { get; }
    bool IsGameProcessOptimizationSupported { get; }
    nint PerformanceProcessorAffinity { get; }
    bool ProcessorsHavePerformanceVarianceAndConfigurableAffinity { get; }
    int ProgressMaximum { get; set; }
    AppProgressState ProgressState { get; set; }
    int ProgressValue { get; set; }

    Task<bool> ForegroundGameAsync(DirectoryInfo installationDirectory);
    Task<Process?> GetGameProcessAsync(DirectoryInfo installationDirectory);
    Task<string> GetTimezoneIanaNameAsync();
    Task<Version?> GetTS4InstallationVersionAsync();
    Task<bool> SendLocalNotificationAsync(string caption, string text);
    Task<bool> SetBadgeNumberAsync(int number);
    void ViewDirectory(DirectoryInfo directoryInfo);
    void ViewFile(FileInfo fileInfo);
}
