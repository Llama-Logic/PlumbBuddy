namespace PlumbBuddy.Services.ScriptApi;

public class SpecificBridgedUiMessageSentEventArgs :
    BridgedUiMessageSentEventArgs
{
    public required Guid UniqueId { get; init; }
}
