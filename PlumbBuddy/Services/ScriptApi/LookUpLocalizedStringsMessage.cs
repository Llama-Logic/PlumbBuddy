namespace PlumbBuddy.Services.ScriptApi;

public class LookUpLocalizedStringsMessage
{
    public IList<byte> Locales { get; } = [];
    public IList<uint> LocKeys { get; } = [];
    public Guid LookUpId { get; set; }
}
