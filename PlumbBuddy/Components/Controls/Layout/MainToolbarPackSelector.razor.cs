namespace PlumbBuddy.Components.Controls.Layout;

partial class MainToolbarPackSelector
{
    bool packSelectorLocked = false;

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
            RefreshPackSelectorLocked();
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            RefreshPackSelectorLocked();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GameResourceCataloger.PropertyChanged += HandleGameResourceCatalogerPropertyChanged;
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        packSelectorLocked = ShouldPackSelectorBeLocked();
    }

    void RefreshPackSelectorLocked()
    {
        var newPackSelectorLocked = ShouldPackSelectorBeLocked();
        if (newPackSelectorLocked != packSelectorLocked)
        {
            StaticDispatcher.Dispatch(() =>
            {
                packSelectorLocked = newPackSelectorLocked;
                StateHasChanged();
            });
        }
    }

    bool ShouldPackSelectorBeLocked() =>
        ModsDirectoryCataloger.State is not ModsDirectoryCatalogerState.Idle or ModsDirectoryCatalogerState.Sleeping || GameResourceCataloger.PackageExaminationsRemaining is not 0;
}
