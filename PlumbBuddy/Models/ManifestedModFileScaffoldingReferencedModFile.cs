namespace PlumbBuddy.Models;

public sealed class ManifestedModFileScaffoldingReferencedModFile
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitNull, Description = "this is the path on your local system relative to the referenced mod file from your mod's file at the last time I saw it")]
    public string? LocalRelativePath { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "this is the absolute path on your local system relative to the referenced mod file at the last time I saw it")]
    public required string LocalAbsolutePath { get; set; }
}
