using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PlumbBuddy.Platforms.Windows;

partial class DesktopInputInterceptor :
    IDesktopInputInterceptor
{
    public DesktopInputInterceptor(IPlatformFunctions platformFunctions, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        hookProcedureDelegate = HookProcedure;
        GetGameProcess();
        Task.Run(ProcessHookMessageQueueAsync);
    }

    ~DesktopInputInterceptor() =>
        Dispose(false);

    Process? gameProcess;
    readonly AsyncLock gameProcessLock = new();
    UnhookWindowsHookExSafeHandle? hookHandle;
    readonly AsyncLock hookHandleLock = new();
    readonly AsyncProducerConsumerQueue<(bool isDown, KBDLLHOOKSTRUCT info)> hookMessageQueue = new();
    readonly HOOKPROC hookProcedureDelegate;
    readonly ConcurrentDictionary<DesktopInputKey, bool> keys = [];
    readonly HashSet<DesktopInputKey> keysCurrentlyDown = [];
    readonly AsyncLock keysCurrentlyDownLock = new();
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;

    /// <summary>
    /// Gets the keys collection of the internal dictionary.
    /// Note: ICollection implies mutability that isn't there; try mutation and get exceptions thrown at your ass.
    /// </summary>
    public ICollection<DesktopInputKey> MonitoredKeys =>
        keys.Keys;

    public event EventHandler<DesktopInputEventArgs>? KeyDown;
    public event EventHandler<DesktopInputEventArgs>? KeyUp;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            hookMessageQueue.CompleteAdding();
            using var hookHandleLockHeld = hookHandleLock.Lock();
            hookHandle?.Dispose();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        }
    }

    void GetGameProcess() =>
        Task.Run(GetGameProcessAsync);

    async Task GetGameProcessAsync()
    {
        var clearKeys = false;
        using (var gameProcessLockHeld = await gameProcessLock.LockAsync().ConfigureAwait(false))
        {
            try
            {
                gameProcess = null;
                if (modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Sleeping) // only happens when the game is running
                {
                    var installationFolder = new DirectoryInfo(settings.InstallationFolderPath);
                    if (installationFolder.Exists)
                        gameProcess = await platformFunctions.GetGameProcessAsync(installationFolder).ConfigureAwait(false);
                }
            }
            finally
            {
                clearKeys = gameProcess is null;
            }
        }
        if (clearKeys)
        {
            keys.Clear();
            await ManageHookAsync().ConfigureAwait(false);
        }
    }

    async Task<bool> GetIsGameForegroundedAsync()
    {
        int gameProcessId = default;
        using (var gameProcessLockHeld = await gameProcessLock.LockAsync().ConfigureAwait(false))
            gameProcessId = gameProcess?.Id ?? default;
        if (gameProcessId is 0)
            return false;
        var foregroundHwnd = PInvoke.GetForegroundWindow();
        if (foregroundHwnd == default)
            return false;
        // why do this rather than use gameProcess.MainWindowHandle?
        // BCL updates that lazily and I don't fucking trust Maxis
        // sorry, not sorry
        PInvoke.GetWindowThreadProcessId(foregroundHwnd, out var foregroundProcessId);
        return foregroundProcessId == gameProcessId;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            GetGameProcess();
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.InstallationFolderPath))
            GetGameProcess();
    }

    LRESULT HookProcedure(int nCode, WPARAM wParam, LPARAM lParam)
    {
        var isDown = wParam == PInvoke.WM_KEYDOWN || wParam == PInvoke.WM_SYSKEYDOWN;
        if (nCode >= 0 && (isDown || wParam == PInvoke.WM_KEYUP || wParam == PInvoke.WM_SYSKEYUP))
            hookMessageQueue.Enqueue((isDown, Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam)));
        return PInvoke.CallNextHookEx(default, nCode, wParam, lParam);
    }

    async Task ManageHookAsync()
    {
        using var hookHandleLockHeld = hookHandleLock.Lock();
        var tcs = new TaskCompletionSource();
        StaticDispatcher.Dispatch(() =>
        {
            try
            {
                if (hookHandle is null && !keys.IsEmpty)
                {
                    hookHandle = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD_LL, hookProcedureDelegate, default, default);
                    if (hookHandle.IsClosed || hookHandle.IsInvalid)
                    {
                        hookHandle.Dispose();
                        hookHandle = null;
                    }
                }
                else if (keys.IsEmpty)
                {
                    hookHandle?.Dispose();
                    hookHandle = null;
                }
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                tcs.TrySetResult();
            }
        });
        await tcs.Task.ConfigureAwait(false);
    }

    async Task ProcessHookMessageQueueAsync()
    {
        while (await hookMessageQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var (isDown, info) = await hookMessageQueue.DequeueAsync().ConfigureAwait(false);
            var key = info.GetDesktopInputKey();
            using (var keysCurrentlyDownLockHeld = await keysCurrentlyDownLock.LockAsync().ConfigureAwait(false))
            {
                if (isDown == keysCurrentlyDown.Contains(key))
                    continue;
                if (isDown)
                    keysCurrentlyDown.Add(key);
                else
                    keysCurrentlyDown.Remove(key);
            }
            if (await GetIsGameForegroundedAsync().ConfigureAwait(false))
            {
                if (key is not DesktopInputKey.None && keys.ContainsKey(key))
                {
                    var eventArgs = new DesktopInputEventArgs { Key = key };
                    if (isDown)
                        KeyDown?.Invoke(this, eventArgs);
                    else
                        KeyUp?.Invoke(this, eventArgs);
                }
            }
        }
    }

    public async Task<bool> StartMonitoringKeyAsync(DesktopInputKey key)
    {
        if (key is DesktopInputKey.None)
            throw new ArgumentOutOfRangeException(nameof(key), $"cannot be {nameof(DesktopInputKey.None)}");
        var added = keys.TryAdd(key, true);
        if (added)
            await ManageHookAsync().ConfigureAwait(false);
        return added;
    }

    public async Task<bool> StopMonitoringKeyAsync(DesktopInputKey key)
    {
        if (key is DesktopInputKey.None)
            throw new ArgumentOutOfRangeException(nameof(key), $"cannot be {nameof(DesktopInputKey.None)}");
        var removed = keys.TryRemove(key, out _);
        if (removed)
            await ManageHookAsync().ConfigureAwait(false);
        return removed;
    }
}
