namespace PlumbBuddy.Services;

public class ProxyHostClientConnectionEventArgs :
    EventArgs
{
    public required TcpClient Client { get; init; }
}
