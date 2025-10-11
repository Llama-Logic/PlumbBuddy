namespace PlumbBuddy.Services.Input;

public interface IObservableButton :
    INotifyPropertyChanged
{
    string Name { get; }
    bool Pressed { get; }

    event EventHandler<ButtonUpdatedEventArgs>? ButtonUpdated;
}
