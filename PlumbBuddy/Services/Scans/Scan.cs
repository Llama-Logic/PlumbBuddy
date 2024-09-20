namespace PlumbBuddy.Services.Scans;

public abstract class Scan :
    IScan
{
    ~Scan() =>
        Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual IAsyncEnumerable<ScanIssue> ScanAsync() =>
        AsyncEnumerable.Empty<ScanIssue>();

    public virtual Task ResolveIssueAsync(object issueData, object resolutionData) =>
        Task.CompletedTask;

    protected virtual void Dispose(bool disposing)
    {
    }
}
