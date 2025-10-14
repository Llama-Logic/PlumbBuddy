namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadsResetMessage :
    HostMessageBase
{
    public required IReadOnlyList<GamepadsResetMessageGamepad> Gamepads { get; set; }
}
