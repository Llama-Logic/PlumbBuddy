namespace PlumbBuddy.Components.Controls.Layout;

partial class MainToolbarPackSelector
{
    public MainToolbarPackSelector() =>
        packSelectorLockedDebouncer = new(RefreshPackSelectorLockedAsync, TimeSpan.FromSeconds(0.1));

    bool packSelectorLocked = false;
    readonly AsyncDebouncer packSelectorLockedDebouncer;

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
        if (e.PropertyName is nameof(IGameResourceCataloger.PackageExaminationsRemaining))
            packSelectorLockedDebouncer.Execute();
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            packSelectorLockedDebouncer.Execute();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GameResourceCataloger.PropertyChanged += HandleGameResourceCatalogerPropertyChanged;
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        packSelectorLocked = ShouldPackSelectorBeLocked();
    }

    async Task RefreshPackSelectorLockedAsync()
    {
        var newPackSelectorLocked = ShouldPackSelectorBeLocked();
        if (newPackSelectorLocked != packSelectorLocked)
        {
            await StaticDispatcher.DispatchAsync(() =>
            {
                packSelectorLocked = newPackSelectorLocked;
                StateHasChanged();
            }).ConfigureAwait(false);
        }
    }

    bool ShouldPackSelectorBeLocked()
    {
        if (ModsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle && GameResourceCataloger.PackageExaminationsRemaining is 0)
            return false;
        if (ModsDirectoryCataloger.State is ModsDirectoryCatalogerState.Sleeping)
            return false;
        return true;
    }
}
