using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using IWindow = Silk.NET.Windowing.IWindow;
using Window = Silk.NET.Windowing.Window;

namespace PlumbBuddy.Platforms.Windows.Input;

public sealed partial class GamepadInterop :
    IGamepadInterop
{
    public GamepadInterop(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
        gamepads = [];
        gamepadsLock = new();
        Gamepads = new(gamepads);
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        if (this.settings.ConnectToGamePads)
            Connect();
    }

    ~GamepadInterop() =>
        Dispose(false);

    readonly ObservableRangeCollection<IObservableGamepad> gamepads;
    readonly AsyncLock gamepadsLock;
    IInputContext? input;
    readonly ISettings settings;
    IWindow? window;
    CancellationTokenSource? windowCts;

    public ReadOnlyObservableCollection<IObservableGamepad> Gamepads { get; }

    public event EventHandler? Updated;

    void Connect()
    {
        if (window is not null ||
            windowCts is not null)
            throw new InvalidOperationException("Already connected");
        windowCts = new();
        var windowOptions = WindowOptions.Default;
        windowOptions.Size = new Vector2D<int>(1, 1);
        windowOptions.Title = "PlumbBuddy Input Host";
        windowOptions.IsVisible = false;
        windowOptions.TransparentFramebuffer = true;
        windowOptions.WindowBorder = WindowBorder.Hidden;
        window = Window.Create(windowOptions);
        window.Load += HandleWindowLoad;
        window.Closing += HandleWindowClosing;
        _ = Task.Run(() => window.Run(), windowCts.Token);
    }

    void Disconnect()
    {
        if (windowCts is not null)
        {
            windowCts.Cancel();
            windowCts.Dispose();
            windowCts = null;
        }
        DisposeInput();
        if (window is not null)
        {
            window.Invoke(() => window.Close());
            window.Load -= HandleWindowLoad;
            window.Closing -= HandleWindowClosing;
            window.Dispose();
            window = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disconnect();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
        }
    }

    void DisposeInput()
    {
        if (input is null)
            return;
        using (var heldGamepadsLock = gamepadsLock.Lock())
        {
            foreach (var observableGamepad in gamepads.Cast<ObservableGamepad>())
            {
                observableGamepad.Updated -= HandleObservableGamepadUpdated;
                observableGamepad.Dispose();
            }
            gamepads.Clear();
        }
        input.ConnectionChanged -= HandleInputConnectionChanged;
        input.Dispose();
        input = null;
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ConnectToGamePads))
        {
            if (settings.ConnectToGamePads)
                Connect();
            else
                Disconnect();
        }
    }

    void HandleInputConnectionChanged(IInputDevice inputDevice, bool isConnected)
    {
        if (inputDevice is not IGamepad gamepad)
            return;
        using var heldGamepadsLock = gamepadsLock.Lock();
        if (isConnected)
        {
            if (gamepads.Cast<ObservableGamepad>().Any(og => og.Gamepad == gamepad))
                return;
            var observableGamepad = new ObservableGamepad(gamepad);
            observableGamepad.Updated += HandleObservableGamepadUpdated;
            gamepads.Add(observableGamepad);
        }
        else
        {
            var removedObservableGamepads = gamepads.GetAndRemoveAll(og => ((ObservableGamepad)og).Gamepad == gamepad);
            foreach (var removedObservableGamepad in removedObservableGamepads.Cast<ObservableGamepad>())
            {
                removedObservableGamepad.Updated -= HandleObservableGamepadUpdated;
                removedObservableGamepad.Dispose();
            }
        }
    }

    void HandleObservableGamepadUpdated(object? sender, EventArgs e) =>
        Updated?.Invoke(this, EventArgs.Empty);

    void HandleWindowClosing() =>
        DisposeInput();

    void HandleWindowLoad()
    {
        if (window is null)
            throw new NullReferenceException("window is null");
        input = window.CreateInput();
        using (var heldGamepadsLock = gamepadsLock.Lock())
            foreach (var gamepad in input.Gamepads)
            {
                var observableGamepad = new ObservableGamepad(gamepad);
                observableGamepad.Updated += HandleObservableGamepadUpdated;
                gamepads.Add(observableGamepad);
            }
        input.ConnectionChanged += HandleInputConnectionChanged;
    }
}
