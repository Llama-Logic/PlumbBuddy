namespace PlumbBuddy.Services.ScriptApi;

public class KeyInterceptionResponseMessage :
    HostMessageBase
{
    public required IReadOnlyDictionary<int, int> KeyResults { get; set; }
    public Guid? RequestId { get; set; }
}
