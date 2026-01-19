namespace PlumbBuddy.Services.ScriptApi;

public class KeyInterceptionMessage
{
    public required IReadOnlyList<int> Keys { get; set; }
    public Guid RequestId { get; set; }
}
