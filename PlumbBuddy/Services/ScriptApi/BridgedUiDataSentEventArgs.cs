namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiDataSentEventArgs :
    BridgedUiMessageSentEventArgs
{
    public required Guid Recipient { get; set; }
}
