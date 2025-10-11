namespace PlumbBuddy.Services.Input;

public interface IObservableThumbstick :
    INotifyPropertyChanged
{
    float Direction { get; }
    float Position { get; }
    float X { get; }
    float Y { get; }

    event EventHandler<ThumbstickUpdatedEventArgs>? ThumbstickUpdated;
}
