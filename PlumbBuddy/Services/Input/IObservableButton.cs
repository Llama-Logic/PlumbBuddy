namespace PlumbBuddy.Services.Input;

public interface IObservableButton :
    INotifyPropertyChanged
{
    IObservableGamepad Gamepad { get; }
    string Name { get; }
    bool Pressed { get; }

    event EventHandler<ButtonUpdatedEventArgs>? ButtonUpdated;
}
