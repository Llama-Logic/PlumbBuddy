namespace PlumbBuddy.Services.ScriptApi;

public class OpenUrlMessage
{
    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings", Justification = "Would confuse the serializer, sorry.")]
    public string? Url { get; set; }
}
