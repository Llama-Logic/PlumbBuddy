namespace PlumbBuddy.Services.ScriptApi;

public class LookUpLocalizedStringsResponseEntry
{
    public byte Locale { get; set; }
    public uint LocKey { get; set; }
    public required string Value { get; set; }
}
