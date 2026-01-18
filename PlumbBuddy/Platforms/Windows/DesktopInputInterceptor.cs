using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PlumbBuddy.Platforms.Windows;

partial class DesktopInputInterceptor :
    IDesktopInputInterceptor
{
    static DesktopInputKey GetDesktopInputKey(KBDLLHOOKSTRUCT kbDllHookStruct) =>
        (VIRTUAL_KEY)kbDllHookStruct.vkCode switch
        {
            VIRTUAL_KEY.VK_BACK => DesktopInputKey.Backspace,
            VIRTUAL_KEY.VK_RETURN => DesktopInputKey.Enter,
            VIRTUAL_KEY.VK_SPACE => DesktopInputKey.Space,
            VIRTUAL_KEY.VK_PRIOR => DesktopInputKey.PageUp,
            VIRTUAL_KEY.VK_NEXT => DesktopInputKey.PageDown,
            VIRTUAL_KEY.VK_END => DesktopInputKey.End,
            VIRTUAL_KEY.VK_HOME => DesktopInputKey.Home,
            VIRTUAL_KEY.VK_LEFT => DesktopInputKey.Left,
            VIRTUAL_KEY.VK_UP => DesktopInputKey.Up,
            VIRTUAL_KEY.VK_RIGHT => DesktopInputKey.Right,
            VIRTUAL_KEY.VK_DOWN => DesktopInputKey.Down,
            VIRTUAL_KEY.VK_INSERT => DesktopInputKey.Insert,
            VIRTUAL_KEY.VK_DELETE => DesktopInputKey.Delete,
            VIRTUAL_KEY.VK_OEM_3 => DesktopInputKey.Grave,
            VIRTUAL_KEY.VK_OEM_MINUS => DesktopInputKey.Minus,
            VIRTUAL_KEY.VK_OEM_PLUS => DesktopInputKey.Equals,
            VIRTUAL_KEY.VK_OEM_4 => DesktopInputKey.LeftBracket,
            VIRTUAL_KEY.VK_OEM_6 => DesktopInputKey.RightBracket,
            VIRTUAL_KEY.VK_OEM_5 => DesktopInputKey.Backslash,
            VIRTUAL_KEY.VK_OEM_1 => DesktopInputKey.Semicolon,
            VIRTUAL_KEY.VK_OEM_7 => DesktopInputKey.Apostrophe,
            VIRTUAL_KEY.VK_OEM_COMMA => DesktopInputKey.Comma,
            VIRTUAL_KEY.VK_OEM_PERIOD => DesktopInputKey.Period,
            VIRTUAL_KEY.VK_OEM_2 => DesktopInputKey.ForwardSlash,
            VIRTUAL_KEY.VK_0 => DesktopInputKey.Number0,
            VIRTUAL_KEY.VK_1 => DesktopInputKey.Number1,
            VIRTUAL_KEY.VK_2 => DesktopInputKey.Number2,
            VIRTUAL_KEY.VK_3 => DesktopInputKey.Number3,
            VIRTUAL_KEY.VK_4 => DesktopInputKey.Number4,
            VIRTUAL_KEY.VK_5 => DesktopInputKey.Number5,
            VIRTUAL_KEY.VK_6 => DesktopInputKey.Number6,
            VIRTUAL_KEY.VK_7 => DesktopInputKey.Number7,
            VIRTUAL_KEY.VK_8 => DesktopInputKey.Number8,
            VIRTUAL_KEY.VK_9 => DesktopInputKey.Number9,
            VIRTUAL_KEY.VK_A => DesktopInputKey.A,
            VIRTUAL_KEY.VK_B => DesktopInputKey.B,
            VIRTUAL_KEY.VK_C => DesktopInputKey.C,
            VIRTUAL_KEY.VK_D => DesktopInputKey.D,
            VIRTUAL_KEY.VK_E => DesktopInputKey.E,
            VIRTUAL_KEY.VK_F => DesktopInputKey.F,
            VIRTUAL_KEY.VK_G => DesktopInputKey.G,
            VIRTUAL_KEY.VK_H => DesktopInputKey.H,
            VIRTUAL_KEY.VK_I => DesktopInputKey.I,
            VIRTUAL_KEY.VK_J => DesktopInputKey.J,
            VIRTUAL_KEY.VK_K => DesktopInputKey.K,
            VIRTUAL_KEY.VK_L => DesktopInputKey.L,
            VIRTUAL_KEY.VK_M => DesktopInputKey.M,
            VIRTUAL_KEY.VK_N => DesktopInputKey.N,
            VIRTUAL_KEY.VK_O => DesktopInputKey.O,
            VIRTUAL_KEY.VK_P => DesktopInputKey.P,
            VIRTUAL_KEY.VK_Q => DesktopInputKey.Q,
            VIRTUAL_KEY.VK_R => DesktopInputKey.R,
            VIRTUAL_KEY.VK_S => DesktopInputKey.S,
            VIRTUAL_KEY.VK_T => DesktopInputKey.T,
            VIRTUAL_KEY.VK_U => DesktopInputKey.U,
            VIRTUAL_KEY.VK_V => DesktopInputKey.V,
            VIRTUAL_KEY.VK_W => DesktopInputKey.W,
            VIRTUAL_KEY.VK_X => DesktopInputKey.X,
            VIRTUAL_KEY.VK_Y => DesktopInputKey.Y,
            VIRTUAL_KEY.VK_Z => DesktopInputKey.Z,
            VIRTUAL_KEY.VK_NUMPAD0 => DesktopInputKey.NumberPad0,
            VIRTUAL_KEY.VK_NUMPAD1 => DesktopInputKey.NumberPad1,
            VIRTUAL_KEY.VK_NUMPAD2 => DesktopInputKey.NumberPad2,
            VIRTUAL_KEY.VK_NUMPAD3 => DesktopInputKey.NumberPad3,
            VIRTUAL_KEY.VK_NUMPAD4 => DesktopInputKey.NumberPad4,
            VIRTUAL_KEY.VK_NUMPAD5 => DesktopInputKey.NumberPad5,
            VIRTUAL_KEY.VK_NUMPAD6 => DesktopInputKey.NumberPad6,
            VIRTUAL_KEY.VK_NUMPAD7 => DesktopInputKey.NumberPad7,
            VIRTUAL_KEY.VK_NUMPAD8 => DesktopInputKey.NumberPad8,
            VIRTUAL_KEY.VK_NUMPAD9 => DesktopInputKey.NumberPad9,
            VIRTUAL_KEY.VK_MULTIPLY => DesktopInputKey.Multiply,
            VIRTUAL_KEY.VK_ADD => DesktopInputKey.Add,
            VIRTUAL_KEY.VK_SUBTRACT => DesktopInputKey.Subtract,
            VIRTUAL_KEY.VK_DECIMAL => DesktopInputKey.Decimal,
            VIRTUAL_KEY.VK_DIVIDE => DesktopInputKey.Divide,
            VIRTUAL_KEY.VK_F1 => DesktopInputKey.F1,
            VIRTUAL_KEY.VK_F2 => DesktopInputKey.F2,
            VIRTUAL_KEY.VK_F3 => DesktopInputKey.F3,
            VIRTUAL_KEY.VK_F4 => DesktopInputKey.F4,
            VIRTUAL_KEY.VK_F5 => DesktopInputKey.F5,
            VIRTUAL_KEY.VK_F6 => DesktopInputKey.F6,
            VIRTUAL_KEY.VK_F7 => DesktopInputKey.F7,
            VIRTUAL_KEY.VK_F8 => DesktopInputKey.F8,
            VIRTUAL_KEY.VK_F9 => DesktopInputKey.F9,
            VIRTUAL_KEY.VK_F10 => DesktopInputKey.F10,
            VIRTUAL_KEY.VK_F11 => DesktopInputKey.F11,
            VIRTUAL_KEY.VK_F12 => DesktopInputKey.F12,
            _ => DesktopInputKey.None
        };

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
            var key = GetDesktopInputKey(info);
            if (key is DesktopInputKey.None)
                continue;
            using (var keysCurrentlyDownLockHeld = await keysCurrentlyDownLock.LockAsync().ConfigureAwait(false))
            {
                if (isDown == keysCurrentlyDown.Contains(key))
                    continue;
                if (isDown)
                    keysCurrentlyDown.Add(key);
                else
                    keysCurrentlyDown.Remove(key);
            }
            if (await GetIsGameForegroundedAsync().ConfigureAwait(false)
                && keys.ContainsKey(key))
            {
                var eventArgs = new DesktopInputEventArgs { Key = key };
                if (isDown)
                    KeyDown?.Invoke(this, eventArgs);
                else
                    KeyUp?.Invoke(this, eventArgs);
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
