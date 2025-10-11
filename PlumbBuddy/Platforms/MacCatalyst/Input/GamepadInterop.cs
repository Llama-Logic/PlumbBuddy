using GameController;
using Foundation;

namespace PlumbBuddy.Platforms.MacCatalyst.Input;

public sealed class GamepadInterop :
    IGamepadInterop
{
    public GamepadInterop()
    {
        gamepads = new(GCController.Controllers.Select(controller => new ObservableGamepad(this, controller)));
        Gamepads = new(gamepads);
        didConnectNotificationObserver = NSNotificationCenter.DefaultCenter.AddObserver(GCController.DidConnectNotification, HandleDidConnectNotification);
        didDisconnectNotificationObserver = NSNotificationCenter.DefaultCenter.AddObserver(GCController.DidDisconnectNotification, HandleDidDisconnectNotification);
    }

    ~GamepadInterop() =>
        Dispose(false);

    readonly NSObject didConnectNotificationObserver;
    readonly NSObject didDisconnectNotificationObserver;
    readonly ObservableCollection<IObservableGamepad> gamepads;

    public ReadOnlyObservableCollection<IObservableGamepad> Gamepads { get; }

    public event EventHandler? Updated;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(didConnectNotificationObserver);
            didConnectNotificationObserver.Dispose();
            NSNotificationCenter.DefaultCenter.RemoveObserver(didDisconnectNotificationObserver);
            didDisconnectNotificationObserver.Dispose();
            foreach (var gamepad in gamepads)
                gamepad.Dispose();
        }
    }

    void HandleDidConnectNotification(NSNotification notification)
    {
        if (notification.Object is GCController controller)
            gamepads.Add(new ObservableGamepad(this, controller));
    }

    void HandleDidDisconnectNotification(NSNotification notification)
    {
        if (notification.Object is GCController controller
            && gamepads.Cast<ObservableGamepad>().FirstOrDefault(gamepad => gamepad.Controller == controller) is { } gamepad)
        {
            gamepad.Dispose();
            gamepads.Remove(gamepad);
        }
    }

    internal void RaiseUpdated() =>
        Updated?.Invoke(this, EventArgs.Empty);
}