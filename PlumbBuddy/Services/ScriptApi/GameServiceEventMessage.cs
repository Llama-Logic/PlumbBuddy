namespace PlumbBuddy.Services.ScriptApi;

public class GameServiceEventMessage
{
    public ulong? Created { get; set; }
    public required string Event { get; set; }
    public ulong? NucleusId { get; set; }
    public ulong? SimNow { get; set; }
    public ulong? SlotId { get; set; }
}
