namespace PlumbBuddy.Services.Input;

public interface IObservableGamepad :
    IDisposable
{
    IReadOnlyList<IObservableButton> Buttons { get; }
    string Name { get; }
    IReadOnlyList<IObservableThumbstick> Thumbsticks { get; }
    IReadOnlyList<IObservableTrigger> Triggers { get; }

    event EventHandler? Updated;

    float ApplyDeadzone(float raw);
    Task<bool> VibrateAsync(double intensity, TimeSpan duration);
}
