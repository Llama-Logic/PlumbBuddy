namespace PlumbBuddy.Services.Input;

public interface IGamepadInterop :
    IDisposable
{
    ReadOnlyObservableCollection<ObservableGamepad> Gamepads { get; }

    event EventHandler? Updated;
}
