namespace PlumbBuddy.Services.Input;

public interface IObservableTrigger :
    INotifyPropertyChanged
{
    float Position { get; }
}
