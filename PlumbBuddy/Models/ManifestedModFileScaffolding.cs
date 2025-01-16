namespace PlumbBuddy.Models;

public sealed class ManifestedModFileScaffolding :
    IParsable<ManifestedModFileScaffolding>
{
    public static void DeleteFor(FileInfo modFile)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        var scaffoldingFile = new FileInfo(GetScaffoldingPath(modFile, true));
        if (scaffoldingFile.Exists)
        {
            try
            {
                scaffoldingFile.Delete();
            }
            catch
            {
                // just trying to help... 😟
            }
        }
        scaffoldingFile = new FileInfo(GetScaffoldingPath(modFile, false));
        if (scaffoldingFile.Exists)
        {
            try
            {
                scaffoldingFile.Delete();
            }
            catch
            {
                // just trying to help... 😟
            }
        }
    }

    static string GetScaffoldingPath(FileInfo modFile, bool useScaffoldingSubdirectory)
    {
        var fileName = $"{modFile.Name}.manifest_scaffolding.yml";
        if (useScaffoldingSubdirectory)
            return Path.Combine(modFile.DirectoryName!, "PlumbBuddy", fileName);
        return Path.Combine(modFile.DirectoryName!, fileName);
    }

    public static bool IsModFileScaffolded(FileInfo modFile, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        ArgumentNullException.ThrowIfNull(settings);
        modFile.Refresh();
        if (!modFile.Exists)
            return false;
        var scaffoldingPath = GetScaffoldingPath(modFile, settings.WriteScaffoldingToSubdirectory);
        if (File.Exists(scaffoldingPath))
            return true;
        scaffoldingPath = GetScaffoldingPath(modFile, !settings.WriteScaffoldingToSubdirectory);
        if (File.Exists(scaffoldingPath))
            return true;
        return false;
    }

    public static ManifestedModFileScaffolding Parse(string s) =>
        Parse(s, null);

    public static ManifestedModFileScaffolding Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var key))
            return key;
        throw new FormatException($"Unable to parse '{s}' as {nameof(ManifestedModFileScaffolding)}.");
    }

    public static async Task<ManifestedModFileScaffolding?> TryLoadForAsync(FileInfo modFile, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        ArgumentNullException.ThrowIfNull(settings);
        modFile.Refresh();
        if (!modFile.Exists)
            return null;
        var scaffoldingPath = GetScaffoldingPath(modFile, settings.WriteScaffoldingToSubdirectory);
        if (File.Exists(scaffoldingPath))
        {
            try
            {
                using var scaffoldingStream = new FileStream(scaffoldingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var scaffoldingStreamReader = new StreamReader(scaffoldingStream);
                if (TryParse(await scaffoldingStreamReader.ReadToEndAsync().ConfigureAwait(false), out var scaffolding))
                    return scaffolding;
            }
            catch
            {
            }
        }
        scaffoldingPath = GetScaffoldingPath(modFile, !settings.WriteScaffoldingToSubdirectory);
        if (File.Exists(scaffoldingPath))
        {
            try
            {
                using var scaffoldingStream = new FileStream(scaffoldingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var scaffoldingStreamReader = new StreamReader(scaffoldingStream);
                if (TryParse(await scaffoldingStreamReader.ReadToEndAsync().ConfigureAwait(false), out var scaffolding))
                    return scaffolding;
            }
            catch
            {
            }
        }
        return null;
    }

    public static bool TryParse(string? s, [MaybeNullWhen(false)] out ManifestedModFileScaffolding result) =>
        TryParse(s, null, out result);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ManifestedModFileScaffolding result)
    {
        if (s is not null && Yaml.CreateYamlDeserializer().Deserialize<ManifestedModFileScaffolding>(s) is { } scaffolding)
        {
            result = scaffolding;
            return true;
        }
        result = default;
        return false;
    }

    [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "this tells me what this mod's name is")]
    public string ModName { get; set; } = string.Empty;

    [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.Preserve, Description = "this tells me whether this mod file is required by your mod")]
    public bool IsRequired { get; set; }

    [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, Description = "this tells me what this mod file's special component name is")]
    public string ComponentName { get; set; } = string.Empty;

    [YamlMember(Order = 4, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "this tells me how lenient vs. strict your hashing preference was last")]
    public int HashingLevel { get; set; }

    [YamlMember(Order = 5, DefaultValuesHandling = DefaultValuesHandling.OmitEmptyCollections, Description = "these are pointers to the other mod files which are a part of your mod")]
    public Collection<ManifestedModFileScaffoldingReferencedModFile> OtherModComponents { get; private set; } = [];

    public async Task CommitForAsync(FileInfo modFile, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(modFile);
        ArgumentNullException.ThrowIfNull(settings);
        var scaffoldingSubdirectory = new DirectoryInfo(Path.Combine(modFile.DirectoryName!, "PlumbBuddy"));
        if (settings.WriteScaffoldingToSubdirectory && !scaffoldingSubdirectory.Exists)
        {
            scaffoldingSubdirectory.Create();
            scaffoldingSubdirectory.Refresh();
        }
        var scaffoldingPath = GetScaffoldingPath(modFile, settings.WriteScaffoldingToSubdirectory);
        using var scaffoldingStream = File.Open(scaffoldingPath, FileMode.Create);
        using var scaffoldingStreamWriter = new StreamWriter(scaffoldingStream);
        await scaffoldingStreamWriter.WriteAsync
        (
            $"""
            # I am a Manifest Scaffolding file.
            # I am used by PlumbBuddy's Manifest Editor to make it faster and easier for you to update the manifests in your mods prior to publishing updates to them.
            # Thanks for doing that, by the way — you're making players' and support techs' lives a bit easier!
            # 
            # IMPORTANT:
            # If you need to move me or your mod files anywhere, that's fine, just please keep us together!
            # If you back up your mods (and you should), back me up, too!
            # I do not (and probably should not) be deployed along with your mod, as I do contain information about your computer's file system.

            {ToString()}
            """
        ).ConfigureAwait(false);
        await scaffoldingStreamWriter.FlushAsync().ConfigureAwait(false);
        var otherPotentialScaffoldingFile = new FileInfo(GetScaffoldingPath(modFile, !settings.WriteScaffoldingToSubdirectory));
        if (otherPotentialScaffoldingFile.Exists)
        {
            try
            {
                otherPotentialScaffoldingFile.Delete();
            }
            catch
            {
                // just trying to help... 😟
            }
        }
        if (!settings.WriteScaffoldingToSubdirectory
            && scaffoldingSubdirectory.Exists
            && scaffoldingSubdirectory.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly).Length is 0)
        {
            try
            {
                scaffoldingSubdirectory.Delete(true);
            }
            catch
            {
                // just trying to help... 😟
            }
        }
    }

    public override string ToString() =>
        Yaml.CreateYamlSerializer().Serialize(this);
}
