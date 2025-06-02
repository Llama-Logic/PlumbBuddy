namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiLookUpResponseMessage
{
    public bool IsLoaded { get; set; }
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}
