namespace PlumbBuddy.App.Services;

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
    /// Called when the main window should be hidden
    /// </summary>
    void HideWindow();

    /// <summary>
    /// Called when the main window should be shown
    /// </summary>
    void ShowWindow();

    /// <summary>
    /// Called when the first launched instance of the app first shows its window
    /// </summary>
    void WindowFirstShown(Window window);
}
