namespace PlumbBuddy.Services.Scans;

public class ScanIssue
{
    public required string Caption { get; init; }
    public object? Data { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required IScan Origin { get; init; }
    public IReadOnlyList<ScanIssueResolution>? Resolutions { get; init; }
    public ScanIssueType Type { get; init; }
}
