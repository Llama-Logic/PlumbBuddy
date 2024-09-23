namespace PlumbBuddy.Services.Scans;

public class ScanIssueResolution
{
    public string? CautionCaption { get; init; }
    public string? CautionText { get; init; }
    public MudBlazor.Color Color { get; init; } = MudBlazor.Color.Default;
    public required object Data { get; init; }
    public required string Icon { get; init; }
    public required string Label { get; init; }
    public Uri? Url { get; init; }
}
