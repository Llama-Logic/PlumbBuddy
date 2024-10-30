namespace PlumbBuddy.Models;

public class SupportDiscordTextFilePattern
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public required string Pattern { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool IgnoreCase { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public double AdditionalRecommendationWeight { get; set; }
}
