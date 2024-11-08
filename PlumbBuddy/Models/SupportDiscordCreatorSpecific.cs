namespace PlumbBuddy.Models;

public class SupportDiscordCreatorSpecific
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Collection<ImmutableArray<byte>> ExceptForHashes { get; private set; } = [];

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections)]
    public Dictionary<string, Collection<SupportDiscordStep>> AskForHelpSteps { get; private set; } = [];
}
