namespace PlumbBuddy.Services.ScriptApi;

public sealed class VibrateGamepadMessage
{
    public float Duration { get; set; }
    public int Index { get; set; }
    public float Intensity { get; set; }
}
