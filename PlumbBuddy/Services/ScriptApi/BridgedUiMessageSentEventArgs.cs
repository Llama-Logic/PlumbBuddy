namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiMessageSentEventArgs :
    EventArgs
{
    public required string MessageJson { get; init; }
}
