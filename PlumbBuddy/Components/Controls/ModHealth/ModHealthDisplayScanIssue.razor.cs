namespace PlumbBuddy.Components.Controls.ModHealth;

partial class ModHealthDisplayScanIssue
{
    [Parameter]
    public ScanIssue? ScanIssue { get; set; }

    async Task ResolveAsync(ScanIssue issue, ScanIssueResolution resolution)
    {
        if (issue.Data is not { } issueData)
            return;
        if (resolution.CautionCaption is { } cautionCaption
            && resolution.CautionText is { } cautionText
            && !await DialogService.ShowCautionDialogAsync(cautionCaption, cautionText))
            return;
        if (issue.Origin is { } origin)
        {
            await origin.ResolveIssueAsync(issueData, resolution.Data);
            return;
        }
        if (issueData is "no-scan-issues" && resolution.Data is "open-mod-health-settings")
        {
            await DialogService.ShowSettingsDialogAsync(3).ConfigureAwait(false);
            return;
        }
    }
}
