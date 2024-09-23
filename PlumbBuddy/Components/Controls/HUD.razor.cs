namespace PlumbBuddy.Components.Controls;

partial class HUD
{
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.PackageCount)
            or nameof(IModsDirectoryCataloger.ScriptArchiveCount)
            or nameof(IModsDirectoryCataloger.State))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.Type))
            StateHasChanged();
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.IsModsDisabledGameSettingOn)
            or nameof(ISmartSimObserver.IsScriptModsEnabledGameSettingOn))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }
}
