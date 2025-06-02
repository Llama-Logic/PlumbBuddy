namespace PlumbBuddy.Services.ScriptApi;

public class FocusBridgedUiResponseMessage
{
    public bool Success { get; set; }
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}
