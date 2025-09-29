namespace PlumbBuddy.Services.Input;

public class ThumbstickUpdatedEventArgs :
    EventArgs
{
    public float Direction { get; init; }
    public float Position { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
}
