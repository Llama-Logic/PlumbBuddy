namespace PlumbBuddy.Components.Controls;

partial class ScanIssuesDisplay
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

    async Task ResolveAsync(ScanIssue issue, ScanIssueResolution resolution)
    {
        if (issue.Data is not { } issueData)
            return;
        if (resolution.CautionCaption is { } cautionCaption
            && resolution.CautionText is { } cautionText
            && !await DialogService.ShowCautionDialogAsync(cautionCaption, cautionText))
            return;
        await issue.Origin.ResolveIssueAsync(issueData, resolution.Data);
    }
}
