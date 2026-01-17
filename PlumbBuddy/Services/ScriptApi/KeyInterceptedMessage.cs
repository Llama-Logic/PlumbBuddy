namespace PlumbBuddy.Services.ScriptApi;

public class KeyInterceptedMessage :
    HostMessageBase
{
    public bool IsDown { get; set; }
    public required string Key { get; set; }
}
