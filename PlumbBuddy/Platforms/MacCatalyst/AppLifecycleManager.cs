using Foundation;
using UIKit;
using Microsoft.Maui.Platform;
using PlumbBuddy.App.Services;
using System;
using System.Threading.Tasks;

namespace PlumbBuddy.Platforms.MacCatalyst;

public class AppLifecycleManager :
    IAppLifecycleManager
{
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