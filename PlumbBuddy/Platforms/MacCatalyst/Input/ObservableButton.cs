using Foundation;
using GameController;

namespace PlumbBuddy.Platforms.MacCatalyst.Input;

public sealed class ObservableButton :
    IDisposable,
    IObservableButton
{
    public ObservableButton(ObservableGamepad gamepad, string name, GCControllerButtonInput button)
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(button);
        this.gamepad = gamepad;
        Name = name;
        this.button = button;
        button.ValueChangedHandler = HandleValueChanged;
        pressed = button.IsPressed;
    }

    ~ObservableButton() =>
        Dispose(false);

    readonly GCControllerButtonInput button;
    readonly ObservableGamepad gamepad;
    bool pressed;

    public IObservableGamepad Gamepad =>
        gamepad;

    public string Name { get; }

    public bool Pressed
    {
        get => pressed;
        private set
        {
            if (pressed == value)
                return;
            pressed = value;
            OnPropertyChanged();
            ButtonUpdated?.Invoke(this, new() { Pressed = value });
            gamepad.RaiseUpdated();
        }
    }

    public event EventHandler<ButtonUpdatedEventArgs>? ButtonUpdated;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
            button.ValueChangedHandler = null;
    }

    void HandleValueChanged(GCControllerButtonInput button, float buttonValue, bool pressed) =>
        Pressed = pressed;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}