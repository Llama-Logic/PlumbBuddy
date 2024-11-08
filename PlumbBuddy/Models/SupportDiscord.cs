namespace PlumbBuddy.Models;

public class SupportDiscord
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public Uri? LogoMedia { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool NoWindowsSupport { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool NoMacSupport { get; set; }

    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, string> Description { get; private set; } = [];

    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, Collection<SupportDiscordStep>> AskForHelpSteps { get; private set; } = [];

    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<SupportDiscordTextFilePattern> SupportedTextFilePatterns { get; private set; } = [];

    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, Collection<SupportDiscordStep>> TextFileSubmissionSteps { get; private set; } = [];

    [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, Collection<SupportDiscordStep>> PatchDayHelpSteps { get; private set; } = [];

    [YamlMember(Order = 9, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, SupportDiscordCreatorSpecific> SpecificCreators { get; private set; } = [];
}
