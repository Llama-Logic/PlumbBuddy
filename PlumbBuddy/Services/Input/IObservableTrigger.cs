namespace PlumbBuddy.Services.Input;

public interface IObservableTrigger :
    INotifyPropertyChanged
{
    IObservableGamepad Gamepad { get; }
    float Position { get; }

    event EventHandler<TriggerUpdatedEventArgs>? TriggerUpdated;
}
