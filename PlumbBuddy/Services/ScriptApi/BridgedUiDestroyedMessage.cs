namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiDestroyedMessage :
    HostMessageBase
{
    public Guid UniqueId { get; set; }
}
