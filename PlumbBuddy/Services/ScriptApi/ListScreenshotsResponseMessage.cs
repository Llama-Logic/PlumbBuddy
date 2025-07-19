namespace PlumbBuddy.Services.ScriptApi;

public class ListScreenshotsResponseMessage :
    HostMessageBase
{
    public IList<ListScreenshotsResponseMessageScreenshot> Screenshots { get; } = [];
}
