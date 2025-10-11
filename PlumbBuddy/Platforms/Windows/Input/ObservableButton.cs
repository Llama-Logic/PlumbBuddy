using Button = Silk.NET.Input.Button;

namespace PlumbBuddy.Platforms.Windows.Input;

public sealed class ObservableButton :
    IObservableButton
{
    public ObservableButton(Button button)
    {
        Button = button;
        Name = button.Name.ToString();
        pressed = button.Pressed;
    }

    bool pressed;

    public Button Button { get; }

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
        }
    }

    public event EventHandler<ButtonUpdatedEventArgs>? ButtonUpdated;
    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    internal void UpdateFrom(Button button)
    {
        Pressed = button.Pressed;
        ButtonUpdated?.Invoke(this, new()
        {
            Pressed = button.Pressed
        });
    }
}
