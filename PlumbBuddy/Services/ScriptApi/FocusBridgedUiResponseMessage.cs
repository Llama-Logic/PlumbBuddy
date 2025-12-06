namespace PlumbBuddy.Services.ScriptApi;

public class FocusBridgedUiResponseMessage :
    HostMessageBase
{
    public bool Success { get; set; }
    public Guid UniqueId { get; set; }
}
