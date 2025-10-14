namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadButtonChangedMessage :
    GamepadMessage
{
    public required string Name { get; set; }
    public bool Pressed { get; set; }
}
