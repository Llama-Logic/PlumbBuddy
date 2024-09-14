namespace PlumbBuddy.App.Services;

public interface ISmartSimObserver :
    IDisposable,
    INotifyPropertyChanged
{
    bool IsModsDisabledGameSettingOn { get; }
    bool IsScriptModsEnabledGameSettingOn { get; }

    void ClearCache();
    void OpenModsFolder();
}
