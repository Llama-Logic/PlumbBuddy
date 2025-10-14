namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadTriggerChangedMessage :
    GamepadMessage
{
    public float Position { get; set; }
    public int Trigger { get; set; }
}
