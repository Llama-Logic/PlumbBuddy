namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiAnnouncementMessage :
    HostMessageBase
{
    public dynamic? Announcement { get; init; }
    public required string UniqueId { get; init; }
}
