namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiLookUpResponseMessage :
    HostMessageBase
{
    public bool IsLoaded { get; set; }
    public Guid UniqueId { get; set; }
}
