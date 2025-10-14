namespace PlumbBuddy.Services.ScriptApi;

public sealed class GamepadsResetMessageGamepad
{
    public required IReadOnlyList<IReadOnlyList<object>> Buttons { get; set; }
    public required IReadOnlyList<IReadOnlyList<object>> Thumbsticks { get; set; }
    public required IReadOnlyList<float> Triggers { get; set; }
}
