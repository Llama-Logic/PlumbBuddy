namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiRequestMessage
{
    public string? HostName { get; set; }
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public IList<BridgedUiRequestMessageLayer>? Layers { get; set; }
    public required string RequestorName { get; set; }
    public required string RequestReason { get; set; }
    public string? ScriptMod { get; set; }
    public string? TabIconPath { get; set; }
    public required string TabName { get; set; }
    public required string UiRoot { get; set; }
    public Guid UniqueId { get; set; }
}
