namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadThumbstickChangedMessage :
    GamepadMessage
{
    public float Direction { get; set; }
    public float Position { get; set; }
    public int Thumbstick { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}
