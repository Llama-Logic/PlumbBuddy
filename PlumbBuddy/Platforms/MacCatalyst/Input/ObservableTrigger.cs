using GameController;

namespace PlumbBuddy.Platforms.MacCatalyst.Input;

public sealed class ObservableTrigger :
    IDisposable,
    IObservableTrigger
{
    public ObservableTrigger(ObservableGamepad gamepad, GCControllerButtonInput trigger)
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        ArgumentNullException.ThrowIfNull(trigger);
        this.gamepad = gamepad;
        this.trigger = trigger;
        trigger.ValueChangedHandler = HandleValueChanged;
        position = trigger.Value;
    }

    ~ObservableTrigger() =>
        Dispose(false);

    readonly ObservableGamepad gamepad;
    float position;
    readonly GCControllerButtonInput trigger;

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
            TriggerUpdated?.Invoke(this, new() { Position = position });
            gamepad.RaiseUpdated();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<TriggerUpdatedEventArgs>? TriggerUpdated;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
            trigger.ValueChangedHandler = null;
    }

    void HandleValueChanged(GCControllerButtonInput button, float buttonValue, bool pressed) =>
        Position = buttonValue;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}