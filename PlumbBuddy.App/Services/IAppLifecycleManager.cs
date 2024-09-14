namespace PlumbBuddy.App.Services;

public interface IAppLifecycleManager
{
    /// <summary>
    /// Gets/sets whether the app should prevent casual closing (default is <see langword="true"/>)
    /// </summary>
    bool PreventCasualClosing { get; set; }

    /// <summary>
    /// Called when the main window should be hidden
    /// </summary>
    void HideWindow();

    /// <summary>
    /// Called when the main window should be shown
    /// </summary>
    void ShowWindow();

    /// <summary>
    /// Called by the UI thread when DI is ready but the UI has not yet started, trap it if necessary until it's time to show the window
    /// </summary>
    void TrapUiThreadBeforeStartup();

    /// <summary>
    /// Called when the first launched instance of the app first shows its window
    /// </summary>
    void WindowFirstShown(Window window);
}
