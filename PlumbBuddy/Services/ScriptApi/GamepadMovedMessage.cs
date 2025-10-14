namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadMovedMessage :
    GamepadMessage
{
    public int NewIndex { get; set; }
}
