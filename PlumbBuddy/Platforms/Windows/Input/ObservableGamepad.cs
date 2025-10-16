using Silk.NET.Input;
using Button = Silk.NET.Input.Button;
using Trigger = Silk.NET.Input.Trigger;

namespace PlumbBuddy.Platforms.Windows.Input;

[SuppressMessage("Design", "CA1060: Move pinvokes to native methods class")]
public sealed partial class ObservableGamepad :
    IObservableGamepad
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Style", "IDE1006: Naming Styles")]
    struct XINPUT_VIBRATION
    {
        public ushort wLeftMotorSpeed, wRightMotorSpeed;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    static extern int Get14(int i, out uint _); // packet only

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    static extern int Get910(int i, out uint _);

    [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState")]
    static extern int Set14(int i, ref XINPUT_VIBRATION v);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputSetState")]
    static extern int Set910(int i, ref XINPUT_VIBRATION v);

    static bool XInputTrySetVibration(int index, double intensity)
    {
        var vibration = new XINPUT_VIBRATION
        {
            wLeftMotorSpeed = (ushort)(Math.Clamp(intensity, 0, 1) * ushort.MaxValue),
            wRightMotorSpeed = (ushort)(Math.Clamp(intensity, 0, 1) * ushort.MaxValue)
        };
        return Set14(index, ref Unsafe.AsRef(ref vibration)) == 0 || Set910(index, ref Unsafe.AsRef(ref vibration)) == 0;
    }

    static readonly int?[] xInputSlots = [null, null, null, null];
    static readonly AsyncLock xInputSlotsLock = new();

    public ObservableGamepad(IGamepad gamepad)
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        using (var xInputSlotsLockHeld = xInputSlotsLock.Lock())
        {
            xInputSlot = xInputSlots.IndexOf(null);
            if (xInputSlot >= 0)
                xInputSlots[xInputSlot] = gamepad.Index;
        }
        Gamepad = gamepad;
        Name = gamepad.Name;
        Buttons = gamepad.Buttons.Select(button => new ObservableButton(this, button)).ToList().AsReadOnly();
        Thumbsticks = gamepad.Thumbsticks.Select(thumbstick => new ObservableThumbstick(this, thumbstick)).ToList().AsReadOnly();
        Triggers = gamepad.Triggers.Select(trigger => new ObservableTrigger(this, trigger)).ToList().AsReadOnly();
        gamepad.ButtonDown += HandleGamepadButtonChanged;
        gamepad.ButtonUp += HandleGamepadButtonChanged;
        gamepad.ThumbstickMoved += HandleGamepadThumbstickMoved;
        gamepad.TriggerMoved += HandleGamepadTriggerMoved;
    }

    ~ObservableGamepad() =>
        Dispose(false);

    readonly int xInputSlot;
    CancellationTokenSource? lastVibrateCallCts;

    public IReadOnlyList<IObservableButton> Buttons { get; }

    public IGamepad Gamepad { get; }

    public string Name { get; }

    public IReadOnlyList<IObservableThumbstick> Thumbsticks { get; }

    public IReadOnlyList<IObservableTrigger> Triggers { get; }

    public event EventHandler? Updated;

    public float ApplyDeadzone(float raw) =>
        Gamepad.Deadzone.Apply(raw);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            Gamepad.ButtonDown -= HandleGamepadButtonChanged;
            Gamepad.ButtonUp -= HandleGamepadButtonChanged;
            Gamepad.ThumbstickMoved -= HandleGamepadThumbstickMoved;
            Gamepad.TriggerMoved -= HandleGamepadTriggerMoved;
            if (lastVibrateCallCts is { } previousVibrateCallCts)
            {
                try
                {
                    previousVibrateCallCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // alright then...
                }
                previousVibrateCallCts.Dispose();
                previousVibrateCallCts = null;
            }
            using var xInputSlotsLockHeld = xInputSlotsLock.Lock();
            xInputSlots[xInputSlot] = null;
        }
    }

    void HandleGamepadButtonChanged(IGamepad gamepad, Button button)
    {
        if (Buttons.Cast<ObservableButton>().FirstOrDefault(ob => ob.Button.Index == button.Index) is not { } observableButton)
            return;
        observableButton.UpdateFrom(button);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    void HandleGamepadThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick)
    {
        if (Thumbsticks.Cast<ObservableThumbstick>().FirstOrDefault(ot => ot.Thumbstick.Index == thumbstick.Index) is not { } observableThumbstick)
            return;
        observableThumbstick.UpdateFrom(thumbstick);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    void HandleGamepadTriggerMoved(IGamepad gamepad, Trigger trigger)
    {
        if (Triggers.Cast<ObservableTrigger>().FirstOrDefault(ot => ot.Trigger.Index == trigger.Index) is not { } observableTrigger)
            return;
        observableTrigger.UpdateFrom(trigger);
        Updated?.Invoke(this, EventArgs.Empty);
    }

    public bool Vibrate(double intensity, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero || duration > TimeSpan.FromSeconds(30))
            return false;
        var xInputSlot = this.xInputSlot;
        if (xInputSlot is < 0 or >= 4)
            return false;
        if (lastVibrateCallCts is { } previousVibrateCallCts)
        {
            try
            {
                previousVibrateCallCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // alright then...
            }
            previousVibrateCallCts.Dispose();
            previousVibrateCallCts = null;
        }
        var isVibrating = XInputTrySetVibration(xInputSlot, intensity);
        if (isVibrating)
        {
            var vibrateCallCts = new CancellationTokenSource(duration);
            lastVibrateCallCts = vibrateCallCts;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(duration, vibrateCallCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // another call has come along, off into the wild blue yonder, green thread...
                    return;
                }
                try
                {
                    vibrateCallCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // another call has come along, off into the wild blue yonder, green thread...
                    return;
                }
                vibrateCallCts.Dispose();
                XInputTrySetVibration(xInputSlot, 0);
            });
        }
        return isVibrating;
    }
}
