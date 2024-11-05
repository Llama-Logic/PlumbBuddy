namespace PlumbBuddy.Components.Controls;

partial class HUD
{
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Settings.PropertyChanged -= HandleSettingsPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.PackageCount)
            or nameof(IModsDirectoryCataloger.ScriptArchiveCount)
            or nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Type))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.IsModsDisabledGameSettingOn)
            or nameof(ISmartSimObserver.IsScriptModsEnabledGameSettingOn))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }
}
