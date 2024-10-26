namespace PlumbBuddy.Services.Scans;

public interface IScan :
    IDisposable
{
    IAsyncEnumerable<ScanIssue> ScanAsync();
    Task ResolveIssueAsync(ILifetimeScope interfaceLifetimeScope, object issueData, object resolutionData);
}
