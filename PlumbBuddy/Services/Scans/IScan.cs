namespace PlumbBuddy.Services.Scans;

public interface IScan :
    IDisposable
{
    IAsyncEnumerable<ScanIssue> ScanAsync();
    Task ResolveIssueAsync(object issueData, object resolutionData);
}
