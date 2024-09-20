namespace PlumbBuddy.Components.Controls;

partial class ScansIssuesDisplay
{
    public void Dispose() =>
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.ScanIssues))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(StateHasChanged);
            else
                StateHasChanged();
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }

    async Task ResolveAsync(ScanIssue issue, ScanIssueResolution resolution)
    {
        if (issue.Data is not { } issueData)
            return;
        if (resolution.CautionCaption is { } caption
            && resolution.CautionText is { } text
            && !await DialogService.ShowCautionDialogAsync(caption, text))
            return;
        await issue.Origin.ResolveIssueAsync(issueData, resolution.Data);
    }
}
