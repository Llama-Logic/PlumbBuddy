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
        await issue.Origin.ResolveIssueAsync(issueData, resolution.Data);
    }
}
