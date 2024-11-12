namespace PlumbBuddy.Models;

public class SupportDiscordStep
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public required string Label { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public required string Icon { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public required string Content { get; set; }

    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool ShowGameVersionFileHighlighter { get; set; }

    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool ShowTextFileHighlighter { get; set; }

    [YamlMember(Order = 6, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool ShowAppLogFileHighlighter { get; set; }

    [YamlMember(Order = 7, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool ShowClearCache { get; set; }

    [YamlMember(Order = 8, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public bool ShowStartOver { get; set; }
}
