using Trigger = Silk.NET.Input.Trigger;

namespace PlumbBuddy.Services.Input;

public sealed class ObservableTrigger :
    INotifyPropertyChanged
{
    public ObservableTrigger(Trigger trigger)
    {
        Trigger = trigger;
        position = trigger.Position;
    }

    public Trigger Trigger { get; }

    float position;

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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<TriggerUpdatedEventArgs>? TriggerUpdated;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    internal void UpdateFrom(Trigger trigger)
    {
        Position = trigger.Position;
        TriggerUpdated?.Invoke(this, new()
        {
            Position = trigger.Position
        });
    }
}
