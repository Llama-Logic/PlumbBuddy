namespace PlumbBuddy.Services.ScriptApi;

public class LookUpLocalizedStringsResponseMessage :
    HostMessageBase
{
    public IList<LookUpLocalizedStringsResponseEntry> Entries { get; } = [];
    public Guid LookUpId { get; set; }
}
