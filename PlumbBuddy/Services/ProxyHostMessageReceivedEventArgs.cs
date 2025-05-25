namespace PlumbBuddy.Services;

public class ProxyHostMessageReceivedEventArgs :
    ProxyHostClientConnectionEventArgs
{
    public required JsonDocument Data { get; init; }
}
