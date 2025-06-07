using UIKit;

namespace PlumbBuddy.Platforms.MacCatalyst;

public class AppLifecycleManager :
    IAppLifecycleManager
{
    public bool HideMainWindowAtLaunch =>
        false;

    public bool IsVisible =>
        true;

    public bool PreventCasualClosing { get; set; }

    public Task UiReleaseSignal =>
        Task.CompletedTask;

    public void HideWindow()
    {
    }

    public void ShowWindow()
    {
    }

    public void WindowFirstShown(Window window)
    {
    }
}