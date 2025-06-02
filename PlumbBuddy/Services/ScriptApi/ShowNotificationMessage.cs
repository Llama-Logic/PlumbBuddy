namespace PlumbBuddy.Services.ScriptApi;

public class ShowNotificationMessage
{
    public required string Text { get; set; }
    public string? Title { get; set; }
    public required string Type { get; set; }
}
