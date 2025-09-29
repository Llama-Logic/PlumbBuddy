namespace PlumbBuddy.Services.Input;

public sealed class TriggerUpdatedEventArgs :
    EventArgs
{
    public float Position { get; init; }
}
