namespace PlumbBuddy.Services.ScriptApi;

public class ForegroundPlumbBuddyMessage
{
    public bool PauseGame { get; set; }
    public required string Type { get; set; }
}