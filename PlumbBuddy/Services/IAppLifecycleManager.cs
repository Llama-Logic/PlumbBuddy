namespace PlumbBuddy.Services;

public interface IAppLifecycleManager
{
    /// <summary>
    /// Gets/sets whether the app should prevent casual closing (default is <see langword="true"/>)
    /// </summary>
    bool PreventCasualClosing { get; set; }

    /// <summary>
    /// Gets a task that is completed when it's time for the UI to initialize
    /// </summary>
    Task UiReleaseSignal { get; }

    /// <summary>
    /// Occurs when the app is about to shut down
    /// </summary>
    event EventHandler? ShuttingDown;

    /// <summary>
    /// Called when the main window should be hidden
    /// </summary>
    void HideWindow();

    /// <summary>
    /// Called when the main window should be shown
    /// </summary>
    void ShowWindow();

    /// <summary>
    /// Called when the app is deliberately stopping
    /// </summary>
    void SignalShuttingDown();

    /// <summary>
    /// Called when the first launched instance of the app first shows its window
    /// </summary>
    void WindowFirstShown(Window window);
}
