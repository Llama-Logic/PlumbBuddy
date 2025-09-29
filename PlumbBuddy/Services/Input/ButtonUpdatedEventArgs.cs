namespace PlumbBuddy.Services.Input;

public sealed class ButtonUpdatedEventArgs :
    EventArgs
{
    public bool Pressed { get; init; }
}
