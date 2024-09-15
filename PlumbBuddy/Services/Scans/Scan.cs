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

    protected virtual void Dispose(bool disposing)
    {
    }
}
