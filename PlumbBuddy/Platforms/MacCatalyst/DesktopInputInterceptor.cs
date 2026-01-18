using System.Diagnostics;

namespace PlumbBuddy.Platforms.MacCatalyst;

public partial class DesktopInputInterceptor :
    IDesktopInputInterceptor
{
    [GeneratedRegex(@"^\s*(?<state>(D|U))\s+(?<keyCode>\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex GetAgentKeyboardActivityPattern();

    static DesktopInputKey GetDesktopInputKey(ushort keyCode) =>
        keyCode switch
        {
            // Editing / navigation
            51 => DesktopInputKey.Backspace,   // Delete (backspace)
            36 => DesktopInputKey.Enter,
            49 => DesktopInputKey.Space,
            116 => DesktopInputKey.PageUp,
            121 => DesktopInputKey.PageDown,
            119 => DesktopInputKey.End,
            115 => DesktopInputKey.Home,
            123 => DesktopInputKey.Left,
            126 => DesktopInputKey.Up,
            124 => DesktopInputKey.Right,
            125 => DesktopInputKey.Down,
            114 => DesktopInputKey.Insert,      // Help key
            117 => DesktopInputKey.Delete,      // Forward delete

            // Punctuation
            50 => DesktopInputKey.Grave,
            27 => DesktopInputKey.Minus,
            24 => DesktopInputKey.Equals,
            33 => DesktopInputKey.LeftBracket,
            30 => DesktopInputKey.RightBracket,
            42 => DesktopInputKey.Backslash,
            41 => DesktopInputKey.Semicolon,
            39 => DesktopInputKey.Apostrophe,
            43 => DesktopInputKey.Comma,
            47 => DesktopInputKey.Period,
            44 => DesktopInputKey.ForwardSlash,

            // Number row
            29 => DesktopInputKey.Number0,
            18 => DesktopInputKey.Number1,
            19 => DesktopInputKey.Number2,
            20 => DesktopInputKey.Number3,
            21 => DesktopInputKey.Number4,
            23 => DesktopInputKey.Number5,
            22 => DesktopInputKey.Number6,
            26 => DesktopInputKey.Number7,
            28 => DesktopInputKey.Number8,
            25 => DesktopInputKey.Number9,

            // Letters
            0 => DesktopInputKey.A,
            11 => DesktopInputKey.B,
            8 => DesktopInputKey.C,
            2 => DesktopInputKey.D,
            14 => DesktopInputKey.E,
            3 => DesktopInputKey.F,
            5 => DesktopInputKey.G,
            4 => DesktopInputKey.H,
            34 => DesktopInputKey.I,
            38 => DesktopInputKey.J,
            40 => DesktopInputKey.K,
            37 => DesktopInputKey.L,
            46 => DesktopInputKey.M,
            45 => DesktopInputKey.N,
            31 => DesktopInputKey.O,
            35 => DesktopInputKey.P,
            12 => DesktopInputKey.Q,
            15 => DesktopInputKey.R,
            1 => DesktopInputKey.S,
            17 => DesktopInputKey.T,
            32 => DesktopInputKey.U,
            9 => DesktopInputKey.V,
            13 => DesktopInputKey.W,
            7 => DesktopInputKey.X,
            16 => DesktopInputKey.Y,
            6 => DesktopInputKey.Z,

            // Numpad
            82 => DesktopInputKey.NumberPad0,
            83 => DesktopInputKey.NumberPad1,
            84 => DesktopInputKey.NumberPad2,
            85 => DesktopInputKey.NumberPad3,
            86 => DesktopInputKey.NumberPad4,
            87 => DesktopInputKey.NumberPad5,
            88 => DesktopInputKey.NumberPad6,
            89 => DesktopInputKey.NumberPad7,
            91 => DesktopInputKey.NumberPad8,
            92 => DesktopInputKey.NumberPad9,
            67 => DesktopInputKey.Multiply,
            69 => DesktopInputKey.Add,
            78 => DesktopInputKey.Subtract,
            65 => DesktopInputKey.Decimal,
            75 => DesktopInputKey.Divide,

            // Function keys
            122 => DesktopInputKey.F1,
            120 => DesktopInputKey.F2,
            99 => DesktopInputKey.F3,
            118 => DesktopInputKey.F4,
            96 => DesktopInputKey.F5,
            97 => DesktopInputKey.F6,
            98 => DesktopInputKey.F7,
            100 => DesktopInputKey.F8,
            101 => DesktopInputKey.F9,
            109 => DesktopInputKey.F10,
            103 => DesktopInputKey.F11,
            111 => DesktopInputKey.F12,

            _ => DesktopInputKey.None
        };

    static async Task<string?> GetForegroundBundleAsync()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "/usr/bin/osascript",
            ArgumentList =
            {
                "-e",
                "tell application \"System Events\" to get bundle identifier of first application process whose frontmost is true"
            },
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        if (process is null)
            return null;
        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

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
        GetGameProcess();
        PollForForegroundBundle();
    }

    ~DesktopInputInterceptor() =>
        Dispose(false);

    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "Will eventually be disposed by ProcessAgentOutputAsync")]
    Process? agentProcess;
    CancellationTokenSource? agentProcessCancellationTokenSource;
    readonly AsyncLock agentProcessLock = new();
    string? foregroundedBundle;
    readonly CancellationTokenSource foregroundPollingCancellationTokenSource = new();
    Process? gameProcess;
    readonly AsyncLock gameProcessLock = new();
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
            using var agentProcessLockHeld = agentProcessLock.Lock();
            agentProcessCancellationTokenSource?.Cancel();
            agentProcessCancellationTokenSource?.Dispose();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            foregroundPollingCancellationTokenSource.Cancel();
            foregroundPollingCancellationTokenSource.Dispose();
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
            await ManageAgentProcessAsync().ConfigureAwait(false);
        }
    }

    void PollForForegroundBundle() =>
        Task.Run(PollForForegroundBundleAsync);

    async Task PollForForegroundBundleAsync()
    {
        var canellationToken = foregroundPollingCancellationTokenSource.Token;
        while (!canellationToken.IsCancellationRequested)
        {
            foregroundedBundle = agentProcess is null ? null : await GetForegroundBundleAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
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

    async Task ManageAgentProcessAsync()
    {
        using var agentProcessLockHeld = await agentProcessLock.LockAsync().ConfigureAwait(false);
        if (agentProcess is null && !keys.IsEmpty)
        {
            var agentPath = Path.Combine(Foundation.NSBundle.MainBundle.BundlePath, "Contents", "MacOS", "Agent");
            if (!File.Exists(agentPath))
                return;
            agentProcessCancellationTokenSource = new();
            agentProcess = Process.Start(new ProcessStartInfo(agentPath)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            _ = Task.Run(ProcessAgentOutputAsync);
        }
        else if (agentProcessCancellationTokenSource is not null && keys.IsEmpty)
        {
            agentProcessCancellationTokenSource.Cancel();
            agentProcessCancellationTokenSource.Dispose();
            agentProcessCancellationTokenSource = null;
        }
    }

    async Task ProcessAgentOutputAsync()
    {
        if (agentProcessCancellationTokenSource?.Token is not { } cancellationToken
            || agentProcess?.StandardOutput is not { } agentOutput)
            return;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await agentOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false) is not { } line)
                    break;
                var match = GetAgentKeyboardActivityPattern().Match(line);
                if (!match.Success)
                    continue;
                var key = GetDesktopInputKey(ushort.Parse(match.Groups["keyCode"].Value));
                if (key is DesktopInputKey.None)
                    continue;
                var isDown = match.Groups["state"].Value.Equals("D", StringComparison.OrdinalIgnoreCase);
                using (var keysCurrentlyDownLockHeld = await keysCurrentlyDownLock.LockAsync().ConfigureAwait(false))
                {
                    if (isDown == keysCurrentlyDown.Contains(key))
                        continue;
                    if (isDown)
                        keysCurrentlyDown.Add(key);
                    else
                        keysCurrentlyDown.Remove(key);
                }
                if (foregroundedBundle == "com.ea.mac.thesims4"
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
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            agentProcess?.Kill(true);
            agentProcess?.Dispose();
            agentProcess = null;
        }
    }

    public async Task<bool> StartMonitoringKeyAsync(DesktopInputKey key)
    {
        if (key is DesktopInputKey.None)
            throw new ArgumentOutOfRangeException(nameof(key), $"cannot be {nameof(DesktopInputKey.None)}");
        var added = keys.TryAdd(key, true);
        if (added)
            await ManageAgentProcessAsync().ConfigureAwait(false);
        return added;
    }

    public async Task<bool> StopMonitoringKeyAsync(DesktopInputKey key)
    {
        if (key is DesktopInputKey.None)
            throw new ArgumentOutOfRangeException(nameof(key), $"cannot be {nameof(DesktopInputKey.None)}");
        var removed = keys.TryRemove(key, out _);
        if (removed)
            await ManageAgentProcessAsync().ConfigureAwait(false);
        return removed;
    }
}
