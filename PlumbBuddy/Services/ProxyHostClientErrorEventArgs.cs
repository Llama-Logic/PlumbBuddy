namespace PlumbBuddy.Services;

public class ProxyHostClientErrorEventArgs :
    ProxyHostClientConnectionEventArgs
{
    public required Exception Exception { get; init; }
}
