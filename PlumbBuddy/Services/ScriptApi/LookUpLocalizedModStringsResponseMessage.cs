namespace PlumbBuddy.Services.ScriptApi;

public class LookUpLocalizedModStringsResponseMessage
{
    public IList<LookUpLocalizedModStringsResponseEntry> Entries { get; } = [];
    public Guid LookUpId { get; set; }
    public required string Type { get; set; }
}
