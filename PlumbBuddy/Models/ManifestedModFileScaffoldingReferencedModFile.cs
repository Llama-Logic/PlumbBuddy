namespace PlumbBuddy.Models;

public sealed class ManifestedModFileScaffoldingReferencedModFile
{
    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "this was the manifest hash of the referenced mod file at the last time I saw it")]
    public ImmutableArray<byte> Hash { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "this is the path on your local system relative to the referenced mod file from your mod's file at the last time I saw it")]
    public required string LocalRelativePath { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "this is the absolute path on your local system relative to the referenced mod file at the last time I saw it")]
    public required string LocalAbsolutePath { get; set; }
}
