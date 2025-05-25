namespace PlumbBuddy.Services;

public interface IProxyHost :
    IDisposable,
    INotifyPropertyChanged
{
    event EventHandler<ProxyHostClientConnectionEventArgs>? ClientConnected;
    event EventHandler<ProxyHostClientConnectionEventArgs>? ClientDisconnected;
    event EventHandler<ProxyHostClientErrorEventArgs>? ClientError;
    event EventHandler<ProxyHostMessageReceivedEventArgs>? MessageReceived;

    bool IsClientConnected { get; }
}
