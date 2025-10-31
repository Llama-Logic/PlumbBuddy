namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadConnectedMessage :
    GamepadMessage
{
    public required IReadOnlyList<IReadOnlyList<object>> Buttons { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyList<IReadOnlyList<object>> Thumbsticks { get; set; }
    public required IReadOnlyList<float> Triggers { get; set; }
}
