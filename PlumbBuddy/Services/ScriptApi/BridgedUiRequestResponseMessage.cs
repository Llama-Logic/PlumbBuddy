namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiRequestResponseMessage :
    HostMessageBase
{
    public const int DenialReason_IndexNotFound = 2;
    public const int DenialReason_InvalidHostName = 4;
    public const int DenialReason_None = 0;
    public const int DenialReason_PlayerDeniedRequest = 3;
    public const int DenialReason_ScriptModNotFound = 1;

    public int DenialReason { get; set; }
    public Guid UniqueId { get; set; }
}
