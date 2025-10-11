namespace PlumbBuddy.Services.Input;

public interface IGamepadInterop :
    IDisposable
{
    ReadOnlyObservableCollection<IObservableGamepad> Gamepads { get; }

    event EventHandler? Updated;
}
