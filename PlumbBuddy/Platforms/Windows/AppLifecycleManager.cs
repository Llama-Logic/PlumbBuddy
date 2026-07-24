using Epiforge.Extensions.Collections.Specialized;
using MessagePack;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace PlumbBuddy.Platforms.Windows;

partial class AppLifecycleManager :
    IAppLifecycleManager,
    IDisposable
{
    static unsafe byte[] CurrentDisplayConfigurationHash
    {
        get
        {
            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var byteArray = ArrayPool<byte>.Shared.Rent(24);
            Span<byte> byteSpan = byteArray;
            try
            {
                var displayAreas = new List<(int left, int top, int right, int bottom, uint dpiX, uint dpiY)>();
                BOOL enumerator(HMONITOR monitor, HDC hdc, RECT* rect, LPARAM lParam)
                {
                    if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY).Succeeded)
                        displayAreas.Add((rect->left, rect->top, rect->right, rect->bottom, dpiX, dpiY));
                    return true;
                }
                PInvoke.EnumDisplayMonitors(default, default, enumerator, default);
                foreach (var displayArea in displayAreas.OrderBy(da => da.left).ThenBy(da => da.top))
                {
                    var (left, top, right, bottom, dpiX, dpiY) = displayArea;
                    MemoryMarshal.Write(byteSpan[0..4], in left);
                    MemoryMarshal.Write(byteSpan[4..8], in top);
                    MemoryMarshal.Write(byteSpan[8..12], in right);
                    MemoryMarshal.Write(byteSpan[12..16], in bottom);
                    MemoryMarshal.Write(byteSpan[16..20], in dpiX);
                    MemoryMarshal.Write(byteSpan[20..24], in dpiY);
                    sha256.AppendData(byteArray[0..24]);
                }
                return sha256.GetCurrentHash();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }
    }

    public AppLifecycleManager(MauiWinUIApplication app, ExtendedActivationKind extendedActivationKind)
    {
        if (extendedActivationKind is ExtendedActivationKind.StartupTask && new Services.Settings(Preferences.Default).Onboarded)
        {
            HideMainWindowAtLaunch = true;
            startupTaskTrap = new(false);
        }
        app.UnhandledException += HandleAppUnhandledException;
    }

    ~AppLifecycleManager() =>
        Dispose(false);

    AppWindow? appWindow;
    bool isWindowActive;
    bool preventCasualClosing = true;
    readonly Dictionary<EquatableList<byte>, SavedWindowPlacement> savedWindowPlacements = [];
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
            if (preventCasualClosing == value)
                return;
            preventCasualClosing = value;
            if (!preventCasualClosing)
                SaveWindowPlacement();
        }
    }

    public Task UiReleaseSignal =>
        startupTaskTrap?.WaitAsync() ?? Task.CompletedTask;

    public event EventHandler<Services.AppLifecycleUnhandledExceptionEventArgs>? UnhandledException;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            xamlWindow?.Activated -= HandleWindowActivated;
            appWindow?.Closing -= HandleAppWindowClosing;
        }
    }

    static byte[]? GetLocalByteArraySetting(string key) =>
        ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var valueObj)
        && valueObj is byte[] value
        ? value
        : default;

    T GetLocalSetting<T>(string key, T defaultValue, IFormatProvider? provider = null)
        where T : IFormattable, IParsable<T> =>
        ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var valueStr)
        && T.TryParse(valueStr?.ToString(), provider, out var value)
        ? value
        : defaultValue;

    static void SetLocalSetting(string key, byte[]? value)
    {
        var values = ApplicationData.Current.LocalSettings.Values;
        if (value == default)
            values.Remove(key);
        else if (values.ContainsKey(key))
            values[key] = value;
        else
            values.Add(key, value);
    }

    void HandleAppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        UnhandledException?.Invoke(this, new Services.AppLifecycleUnhandledExceptionEventArgs { Exception = e.Exception });
    }

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
            var displayConfigurationHash = CurrentDisplayConfigurationHash;
            var equatableDisplayConfigurationHash = new EquatableList<byte>(displayConfigurationHash);
            ref var savedWindowPlacementEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(savedWindowPlacements, equatableDisplayConfigurationHash, out _);
            savedWindowPlacementEntry = SavedWindowPlacement.FromWin32WindowPlacement(string.Join(string.Empty, displayConfigurationHash.Select(b => b.ToString("x2"))), windowPlacement);
            SetLocalSetting("SavedWindowPlacements", MessagePackSerializer.Serialize(savedWindowPlacements.Values.ToList()));
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
            PInvoke.SetForegroundWindow(new HWND(WindowNative.GetWindowHandle(xamlWindow)));
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
            if (GetLocalByteArraySetting("SavedWindowPlacements") is { } savedWindowPlacementsByteArray)
            {
                try
                {
                    var savedWindowPlacements = MessagePackSerializer.Deserialize<List<SavedWindowPlacement>>(savedWindowPlacementsByteArray);
                    foreach (var savedWindowPlacement in savedWindowPlacements)
                        this.savedWindowPlacements.Add(new(Convert.FromHexString(savedWindowPlacement.DisplayConfigurationHashHex)), savedWindowPlacement);
                    if (this.savedWindowPlacements.TryGetValue(new(CurrentDisplayConfigurationHash), out var currentSavedWindowPlacement))
                    {
                        var currentWin32WindowPlacement = currentSavedWindowPlacement.ToWin32WindowPlacement();
                        PInvoke.SetWindowPlacement(new(WindowNative.GetWindowHandle(this.xamlWindow)), in currentWin32WindowPlacement);
                    }
                }
                catch
                {
                    // meh
                }
            }
        }
    }
}
