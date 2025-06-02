namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiRequestResponseMessage
{
    public const int DenialReason_IndexNotFound = 2;
    public const int DenialReason_None = 0;
    public const int DenialReason_PlayerDeniedRequest = 3;
    public const int DenialReason_ScriptModNotFound = 1;

    public int DenialReason { get; set; }
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}
