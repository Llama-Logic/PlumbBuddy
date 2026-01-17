namespace PlumbBuddy.Services.ScriptApi;

public class KeyInterceptionMessage
{
    public required IReadOnlyList<string> Keys { get; set; }
    public Guid RequestId { get; set; }
}
