namespace PlumbBuddy.Services.ScriptApi;

public class ShowNotificationMessage :
    HostMessageBase
{
    public string? IconInstance { get; set; }
    public required string Text { get; set; }
    public string? Title { get; set; }
}
