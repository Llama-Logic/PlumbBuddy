using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace PlumbBuddy.Platforms.Windows;

class AppLifecycleManager :
    IAppLifecycleManager,
    IDisposable
{
    public AppLifecycleManager(ExtendedActivationKind extendedActivationKind)
    {
        if (extendedActivationKind is ExtendedActivationKind.StartupTask)
        {
            HideMainWindowAtLaunch = true;
            startupTaskTrap = new(false);
        }
    }

    ~AppLifecycleManager() =>
        Dispose(false);

    AppWindow? appWindow;
    bool isWindowActive;
    bool preventCasualClosing = true;
    readonly AsyncManualResetEvent? startupTaskTrap;
    Microsoft.UI.Xaml.Window? xamlWindow;

    public bool HideMainWindowAtLaunch { get; }

    public bool IsVisible =>
        appWindow is { } nonNullAppWindow
        && nonNullAppWindow.IsVisible;

    public bool PreventCasualClosing
    {
        get => preventCasualClosing;
        set
        {
            preventCasualClosing = value;
            if (!preventCasualClosing)
                SaveWindowPlacement();
        }
    }

    public Task UiReleaseSignal =>
        startupTaskTrap?.WaitAsync() ?? Task.CompletedTask;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (xamlWindow is not null)
                xamlWindow.Activated -= HandleWindowActivated;
            if (appWindow is not null)
                appWindow.Closing -= HandleAppWindowClosing;
        }
    }

    T GetLocalSetting<T>(string key, T defaultValue, IFormatProvider? provider = null)
        where T : IFormattable, IParsable<T> =>
        ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var valueStr)
        && T.TryParse(valueStr?.ToString(), provider, out var value)
        ? value
        : defaultValue;

    void HandleAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs e)
    {
        if (isWindowActive && PreventCasualClosing)
        {
            sender.Hide();
            e.Cancel = true;
            SaveWindowPlacement();
            return;
        }
    }

    void HandleWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e) =>
        isWindowActive = e.WindowActivationState is not Microsoft.UI.Xaml.WindowActivationState.Deactivated;

    public void HideWindow() =>
        appWindow?.Hide();

    void SaveWindowPlacement()
    {
        WINDOWPLACEMENT windowPlacement = default;
        if (PInvoke.GetWindowPlacement(new HWND(WindowNative.GetWindowHandle(xamlWindow)), ref windowPlacement).Value is not 0)
        {
            SetLocalSetting("WindowFlags", (uint)windowPlacement.flags);
            SetLocalSetting("WindowMaxX", windowPlacement.ptMaxPosition.X);
            SetLocalSetting("WindowMaxY", windowPlacement.ptMaxPosition.Y);
            SetLocalSetting("WindowMinX", windowPlacement.ptMinPosition.X);
            SetLocalSetting("WindowMinY", windowPlacement.ptMinPosition.Y);
            SetLocalSetting("WindowNormalL", windowPlacement.rcNormalPosition.left);
            SetLocalSetting("WindowNormalT", windowPlacement.rcNormalPosition.top);
            SetLocalSetting("WindowNormalR", windowPlacement.rcNormalPosition.right);
            SetLocalSetting("WindowNormalB", windowPlacement.rcNormalPosition.bottom);
            SetLocalSetting("WindowShowCmd", (int)windowPlacement.showCmd);
            SetLocalSetting("WindowPlacementSaved", 1);
        }
    }

    void SetLocalSetting<T>(string key, T value, string? format = null, IFormatProvider? formatProvider = null)
        where T : IFormattable, IParsable<T> =>
        ApplicationData.Current.LocalSettings.Values[key] =
            format is null || formatProvider is null
            ? value?.ToString()
            : value?.ToString(format, formatProvider);

    public void ShowWindow()
    {
        if (!(startupTaskTrap?.IsSet ?? true))
        {
            startupTaskTrap.Set();
            return;
        }
        if (appWindow is not null && xamlWindow is not null)
        {
            if (xamlWindow.DispatcherQueue is { } dispatcherQueue && !dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, ShowWindow);
                return;
            }
            appWindow.Show();
            xamlWindow.Activate();
        }
    }

    public void WindowFirstShown(Window window)
    {
        if (window.Handler.PlatformView is Microsoft.UI.Xaml.Window xamlWindow)
        {
            this.xamlWindow = xamlWindow;
            this.xamlWindow.Activated += HandleWindowActivated;
            isWindowActive = true;
            if (startupTaskTrap is not null)
                this.xamlWindow.Activate();
            appWindow = xamlWindow.AppWindow;
            appWindow.Closing += HandleAppWindowClosing;
            if (GetLocalSetting("WindowPlacementSaved", 0) is not 0)
            {
                var windowPlacement = new WINDOWPLACEMENT
                {
                    flags = (WINDOWPLACEMENT_FLAGS)GetLocalSetting("WindowFlags", 0),
                    length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
                    ptMaxPosition = new System.Drawing.Point
                    (
                        GetLocalSetting("WindowMaxX", 0),
                        GetLocalSetting("WindowMaxY", 0)
                    ),
                    ptMinPosition = new System.Drawing.Point
                    (
                        GetLocalSetting("WindowMinX", 0),
                        GetLocalSetting("WindowMinY", 0)
                    ),
                    rcNormalPosition = new RECT
                    (
                        GetLocalSetting("WindowNormalL", 0),
                        GetLocalSetting("WindowNormalT", 0),
                        GetLocalSetting("WindowNormalR", 0),
                        GetLocalSetting("WindowNormalB", 0)
                    ),
                    showCmd = (SHOW_WINDOW_CMD)GetLocalSetting("WindowShowCmd", 0)
                };
                PInvoke.SetWindowPlacement(new HWND(WindowNative.GetWindowHandle(this.xamlWindow)), in windowPlacement);
            }
        }
    }
}
