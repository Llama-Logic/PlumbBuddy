using CoreHaptics;
using GameController;
using Phase;

namespace PlumbBuddy.Platforms.MacCatalyst.Input;

public sealed class ObservableGamepad :
    IObservableGamepad
{
    public ObservableGamepad(GamepadInterop gamepadInterop, GCController controller)
    {
        ArgumentNullException.ThrowIfNull(gamepadInterop);
        ArgumentNullException.ThrowIfNull(controller);
        this.gamepadInterop = gamepadInterop;
        hapticEngine = new(CreateHapticEngine);
        Controller = controller;
        if (controller.ExtendedGamepad is { } gamepad)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            Buttons =
            [
                new ObservableButton(this, "A", gamepad.ButtonA),
                new ObservableButton(this, "B", gamepad.ButtonB),
                new ObservableButton(this, "X", gamepad.ButtonX),
                new ObservableButton(this, "Y", gamepad.ButtonY),
                new ObservableButton(this, "LeftBumper", gamepad.LeftShoulder),
                new ObservableButton(this, "RightBumper", gamepad.RightShoulder),
                ..gamepad.ButtonOptions is { } buttonOptions ? [new ObservableButton(this, "Back", buttonOptions)] : Enumerable.Empty<ObservableButton>(),
                new ObservableButton(this, "Start", gamepad.ButtonMenu),
                ..gamepad.ButtonHome is { } buttonHome ? [new ObservableButton(this, "Home", buttonHome)] : Enumerable.Empty<ObservableButton>(),
                ..gamepad.LeftThumbstickButton is { } leftThumbstickButton ? [new ObservableButton(this, "LeftStick", leftThumbstickButton)] : Enumerable.Empty<ObservableButton>(),
                ..gamepad.RightThumbstickButton is { } rightThumbstickButton ? [new ObservableButton(this, "RightStick", rightThumbstickButton)] : Enumerable.Empty<ObservableButton>(),
                new ObservableButton(this, "DPadUp", gamepad.DPad.Up),
                new ObservableButton(this, "DPadRight", gamepad.DPad.Right),
                new ObservableButton(this, "DPadDown", gamepad.DPad.Down),
                new ObservableButton(this, "DPadLeft", gamepad.DPad.Left),
            ];
            Thumbsticks =
            [
                new ObservableThumbstick(this, gamepad.LeftThumbstick),
                new ObservableThumbstick(this, gamepad.RightThumbstick)
            ];
            Triggers =
            [
                new ObservableTrigger(this, gamepad.LeftTrigger),
                new ObservableTrigger(this, gamepad.RightTrigger)
            ];
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
        else
        {
            Buttons = [];
            Thumbsticks = [];
            Triggers = [];
        }
        Name = controller.VendorName ?? controller.ProductCategory ?? "Gamepad";
    }

    ~ObservableGamepad() =>
        Dispose(false);

    readonly GamepadInterop gamepadInterop;
    readonly Lazy<CHHapticEngine?> hapticEngine;
    ICHHapticPatternPlayer? hapticPatternPlayer;

    public IReadOnlyList<IObservableButton> Buttons { get; }

    internal GCController Controller { get; }

    public string Name { get; }

    public IReadOnlyList<IObservableThumbstick> Thumbsticks { get; }

    public IReadOnlyList<IObservableTrigger> Triggers { get; }

    public event EventHandler? Updated;

    public float ApplyDeadzone(float raw) =>
        raw;

    public CHHapticEngine? CreateHapticEngine()
    {
        if (Controller.Haptics is not { } haptics)
            return null;
        if (haptics.CreateEngine(GCHapticsLocality.Handles) is not { } engine)
            return null;
        if (!engine.Start(out _))
            return null;
        return engine;
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
            if (hapticPatternPlayer is not null)
            {
                hapticPatternPlayer.Stop(0, out _);
                hapticPatternPlayer.Dispose();
            }
            if (this.hapticEngine.IsValueCreated
                && this.hapticEngine.Value is { } hapticEngine)
                hapticEngine.Dispose();
            foreach (var button in Buttons.Cast<ObservableButton>())
                button.Dispose();
            foreach (var thumbstick in Thumbsticks.Cast<ObservableThumbstick>())
                thumbstick.Dispose();
            foreach (var trigger in Triggers.Cast<ObservableTrigger>())
                trigger.Dispose();
        }
    }

    internal void RaiseUpdated()
    {
        Updated?.Invoke(this, EventArgs.Empty);
        gamepadInterop.RaiseUpdated();
    }

    public bool Vibrate(double intensity, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero || duration > TimeSpan.FromSeconds(30))
            return false;
        if (this.hapticEngine.Value is not { } hapticEngine)
            return false;
        if (hapticPatternPlayer is not null
            && !hapticPatternPlayer.Stop(0, out _))
            return false;
        hapticPatternPlayer?.Dispose();
        hapticPatternPlayer = null;
        if (intensity == default)
            return true;
#pragma warning disable CA2000 // Dispose objects before losing scope
        using var hapticEvent = new CHHapticEvent
        (
            CHHapticEventType.HapticContinuous,
            [
                new CHHapticEventParameter(CHHapticEventParameterId.HapticIntensity, (float)intensity),
                new CHHapticEventParameter(CHHapticEventParameterId.HapticSharpness, 0.5f)
            ],
            0,
            (float)duration.TotalSeconds
        );
#pragma warning restore CA2000 // Dispose objects before losing scope
        using var pattern = new CHHapticPattern([hapticEvent], Array.Empty<CHHapticDynamicParameter>(), out _);
        hapticPatternPlayer = hapticEngine.CreatePlayer(pattern, out _);
        if (hapticPatternPlayer is null)
            return false;
        return hapticPatternPlayer.Start(0, out _);
    }
}