namespace PlumbBuddy.Services.ScriptApi;

public class LookUpLocalizedStringsResponseMessage
{
    public IList<LookUpLocalizedStringsResponseEntry> Entries { get; } = [];
    public Guid LookUpId { get; set; }
    public required string Type { get; set; }
}
