namespace PlumbBuddy.Services.ScriptApi;

public class ListScreenshotNamesResponseMessage :
    HostMessageBase
{
    public IList<string> Names { get; } = [];
}
