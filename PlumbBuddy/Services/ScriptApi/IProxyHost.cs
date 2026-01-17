namespace PlumbBuddy.Services.ScriptApi;

public interface IProxyHost :
    IDisposable,
    INotifyPropertyChanged
{
    event EventHandler<BridgedUiAuthorizedEventArgs>? BridgedUiAuthorized;
    event EventHandler<BridgedUiDataSentEventArgs>? BridgedUiDataSent;
    event EventHandler<BridgedUiEventArgs>? BridgedUiDestroyed;
    event EventHandler<BridgedUiEventArgs>? BridgedUiDomLoaded;
    event EventHandler<BridgedUiFocusRequestedEventArgs>? BridgedUiFocusRequested;
    event EventHandler<BridgedUiRequestedEventArgs>? BridgedUiRequested;
    event EventHandler<BridgedUiMessageSentEventArgs>? BridgedUiMessageSent;
    event EventHandler? ProxyConnected;
    event EventHandler? ProxyDisconnected;
    event EventHandler<SpecificBridgedUiMessageSentEventArgs>? SpecificBridgedUiMessageSent;

    bool IsBridgedUiDevelopmentModeEnabled { get; set; }
    bool IsClientConnected { get; }

    void DestroyBridgedUi(Guid uniqueId);
    Task ForegroundPlumbBuddyAsync(bool pauseGame = false);
    Task NotifyScreenshotsChangedAsync();
    Task ProcessMessageFromBridgedUiAsync(Guid uniqueId, string messageJson);
    Task ShowNotificationAsync(string text, string? title = null, ulong? iconInstance = null);
    Task WaitForSavesAccessAsync(CancellationToken cancellationToken = default);
}
