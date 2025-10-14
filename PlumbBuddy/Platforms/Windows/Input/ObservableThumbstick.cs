using Silk.NET.Input;

namespace PlumbBuddy.Platforms.Windows.Input;

public sealed class ObservableThumbstick :
    IObservableThumbstick
{
    public ObservableThumbstick(IObservableGamepad gamepad, Thumbstick thumbstick)
    {
        Gamepad = gamepad;
        Thumbstick = thumbstick;
        direction = thumbstick.Direction;
        position = thumbstick.Position;
        x = thumbstick.X;
        y = thumbstick.Y;
    }

    float direction;
    float position;
    float x;
    float y;

    public IObservableGamepad Gamepad { get; }

    public Thumbstick Thumbstick { get; }

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
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<ThumbstickUpdatedEventArgs>? ThumbstickUpdated;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    internal void UpdateFrom(Thumbstick thumbstick)
    {
        Direction = thumbstick.Direction;
        Position = thumbstick.Position;
        X = thumbstick.X;
        Y = thumbstick.Y;
        ThumbstickUpdated?.Invoke(this, new()
        {
            Direction = thumbstick.Direction,
            Position = thumbstick.Position,
            X = thumbstick.X,
            Y = thumbstick.Y
        });
    }
}
