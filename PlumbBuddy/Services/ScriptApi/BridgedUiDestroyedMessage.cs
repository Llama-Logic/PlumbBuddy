namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiDestroyedMessage
{
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}
