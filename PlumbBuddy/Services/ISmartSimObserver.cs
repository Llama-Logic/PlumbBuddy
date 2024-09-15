namespace PlumbBuddy.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    bool IsModsDisabledGameSettingOn { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }

    void ClearCache();
    void OpenModsFolder();
}
