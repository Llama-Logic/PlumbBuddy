namespace PlumbBuddy.Services.ScriptApi;

public class ListScreenshotsResponseMessage :
    HostMessageBase
{
    public IList<string> Screenshots { get; } = [];
}
