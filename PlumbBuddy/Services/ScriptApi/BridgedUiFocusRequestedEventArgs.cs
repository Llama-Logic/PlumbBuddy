namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiFocusRequestedEventArgs :
    EventArgs
{
    public Guid UniqueId { get; set; }
}
