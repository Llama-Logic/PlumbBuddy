namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiEventArgs :
    EventArgs
{
    public required Guid UniqueId { get; init; }
}
