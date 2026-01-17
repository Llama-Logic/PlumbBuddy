namespace PlumbBuddy.Services;

public interface IDesktopInputInterceptor :
    IDisposable
{
    ICollection<DesktopInputKey> MonitoredKeys { get; }

    event EventHandler<DesktopInputEventArgs>? KeyDown;
    event EventHandler<DesktopInputEventArgs>? KeyUp;

    Task<bool> StartMonitoringKeyAsync(DesktopInputKey key);
    Task<bool> StopMonitoringKeyAsync(DesktopInputKey key);
}
