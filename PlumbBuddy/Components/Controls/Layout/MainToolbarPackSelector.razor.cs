namespace PlumbBuddy.Components.Controls.Layout;

partial class MainToolbarPackSelector
{
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            GameResourceCataloger.PropertyChanged -= HandleGameResourceCatalogerPropertyChanged;
            ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        }
    }

    void HandleGameResourceCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IGameResourceCataloger.PackageExaminationsRemaining)
            && GameResourceCataloger.PackageExaminationsRemaining is 0)
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GameResourceCataloger.PropertyChanged += HandleGameResourceCatalogerPropertyChanged;
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
    }
}
