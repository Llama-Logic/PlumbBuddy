namespace PlumbBuddy.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    bool IsModsDisabledGameSettingOn { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }
    IReadOnlyList<ScanIssue> ScanIssues { get; }

    void ClearCache();
    void OpenDownloadsFolder();
    void OpenModsFolder();
}
