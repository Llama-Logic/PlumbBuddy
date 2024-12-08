namespace PlumbBuddy.Components.Controls;

partial class FullWindowSemiotic
{
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.ScanIssues))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }
}
