using GameController;

namespace PlumbBuddy.Platforms.MacCatalyst.Input;

public sealed class ObservableThumbstick :
    IDisposable,
    IObservableThumbstick
{
    public ObservableThumbstick(ObservableGamepad gamepad, GCControllerDirectionPad thumbstick)
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        ArgumentNullException.ThrowIfNull(thumbstick);
        this.gamepad = gamepad;
        this.thumbstick = thumbstick;
        thumbstick.XAxis.ValueChangedHandler = HandleXAxisValueChanged;
        thumbstick.YAxis.ValueChangedHandler = HandleYAxisValueChanged;
    }

    ~ObservableThumbstick() =>
        Dispose(false);

    float direction;
    readonly ObservableGamepad gamepad;
    float position;
    readonly GCControllerDirectionPad thumbstick;
    float x;
    float y;

    public float Direction
    {
        get => direction;
        private set
        {
            if (direction == value)
                return;
            direction = value;
            OnPropertyChanged();
        }
    }

    public IObservableGamepad Gamepad =>
        gamepad;

    public float Position
    {
        get => position;
        private set
        {
            if (position == value)
                return;
            position = value;
            OnPropertyChanged();
        }
    }

    public float X
    {
        get => x;
        private set
        {
            if (x == value)
                return;
            x = value;
            OnPropertyChanged();
            CalculateDirectionAndPosition();
        }
    }

    public float Y
    {
        get => y;
        private set
        {
            if (y == value)
                return;
            y = value;
            OnPropertyChanged();
            CalculateDirectionAndPosition();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<ThumbstickUpdatedEventArgs>? ThumbstickUpdated;

    void CalculateDirectionAndPosition()
    {
        Direction = (float)Math.Atan2(y, x);
        Position = (float)Math.Sqrt(x * x + y * y);
        ThumbstickUpdated?.Invoke(this, new()
        {
            Direction = direction,
            Position = position,
            X = x,
            Y = y
        });
        gamepad.RaiseUpdated();
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
            thumbstick.XAxis.ValueChangedHandler = null;
            thumbstick.YAxis.ValueChangedHandler = null;
        }
    }

    void HandleXAxisValueChanged(GCControllerAxisInput axis, float value) =>
        X = value;

    void HandleYAxisValueChanged(GCControllerAxisInput axis, float value) =>
        Y = value;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}