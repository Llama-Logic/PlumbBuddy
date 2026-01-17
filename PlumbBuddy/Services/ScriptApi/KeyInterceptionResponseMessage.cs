namespace PlumbBuddy.Services.ScriptApi;

public class KeyInterceptionResponseMessage :
    HostMessageBase
{
    public required IReadOnlyDictionary<string, int> KeyResults { get; set; }
    public Guid? RequestId { get; set; }
}
