namespace PlumbBuddy.Models;

public sealed class ManifestedModFileScaffolding :
    IParsable<ManifestedModFileScaffolding>
{
    static TBuilder ConfigureBuilder<TBuilder>(TBuilder builder)
        where TBuilder : BuilderSkeleton<TBuilder> => builder
            .WithTypeConverter(new YamlHashHexConverter())
            .WithNamingConvention(UnderscoredNamingConvention.Instance);

    static IDeserializer CreateYamlDeserializer() =>
        ConfigureBuilder(new DeserializerBuilder()).Build();

    static ISerializer CreateYamlSerializer() =>
        ConfigureBuilder(new SerializerBuilder()).Build();

    static string GetScaffoldingPath(FileInfo modFile) =>
        $"{modFile.FullName}.manifestScaffolding.yaml";

    public static ManifestedModFileScaffolding Parse(string s) =>
        Parse(s, null);

    public static ManifestedModFileScaffolding Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(ManifestedModFileScaffolding)}.");
    }

    public static async Task<ManifestedModFileScaffolding?> TryLoadForAsync(FileInfo modFile)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        modFile.Refresh();
        if (!modFile.Exists)
            return null;
        if (TryParse(await File.ReadAllTextAsync(GetScaffoldingPath(modFile)).ConfigureAwait(false), out var scaffolding))
            return scaffolding;
        return null;
    }

    public static bool TryParse(string? s, [MaybeNullWhen(false)] out ManifestedModFileScaffolding result) =>
        TryParse(s, null, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ManifestedModFileScaffolding result)
    {
        if (s is not null && CreateYamlDeserializer().Deserialize<ManifestedModFileScaffolding>(s) is { } scaffolding)
        {
            result = scaffolding;
            return true;
        }
        result = default;
        return false;
    }

    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "this was the manifest hash of this mod file at the last time you inscribed it")]
    public ImmutableArray<byte> Hash { get; set; }

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "whether or not your mod will work without this file installed")]
    public bool IsPlayerOptional { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "pointers to the other mod files which are a part of your mod")]
    public Collection<ManifestedModFileScaffoldingReferencedModFile> OtherModComponents { get; private set; } = [];

    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "pointers to the other mod files which are not a part of your mod, but which your mod requires")]
    public Collection<ManifestedModFileScaffoldingReferencedModFile> Dependencies { get; private set; } = [];

    public async Task CommitForAsync(FileInfo modFile)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        var scaffoldingPath = GetScaffoldingPath(modFile);
        using var scaffoldingStream = File.Open(scaffoldingPath, FileMode.Create);
        using var scaffoldingStreamWriter = new StreamWriter(scaffoldingStream);
        await scaffoldingStreamWriter.WriteAsync
        (
            $"""
            # I am a Manifest Scaffolding file.
            # I am used by Mod Creator tooling to make it faster and easier for you to update the manifests in your mods prior to publishing updates to them.
            # Thanks for doing that, by the way — you're making players' and support techs' lives a bit easier!
            # 
            # IMPORTANT:
            # If you need to move me or your mod files anywhere, that's fine, just please keep us together!
            # If you back up your mods (and you should), back me up, too!
            # I do not (and probably should not) be deployed along with your mod, as I do contain information about your computer's file system.

            {ToString()}
            """
        ).ConfigureAwait(false);
    }

    public override string ToString() =>
        CreateYamlSerializer().Serialize(this);
}
