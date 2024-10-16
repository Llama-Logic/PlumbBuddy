namespace PlumbBuddy.Services;

public class CustomThemes :
    ICustomThemes
{
    public CustomThemes()
    {
        using var customThemesMetadataStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("PlumbBuddy.Metadata.custom-themes.yml");
        if (customThemesMetadataStream is null)
        {
            Themes = new Dictionary<string, CustomTheme>().ToImmutableDictionary();
            return;
        }
        using var customThemesMetadataStreamReader = new StreamReader(customThemesMetadataStream);
        Themes = Yaml.CreateYamlDeserializer().Deserialize<Dictionary<string, CustomTheme>>(customThemesMetadataStreamReader.ReadToEnd()).ToImmutableDictionary();
    }

    public IReadOnlyDictionary<string, CustomTheme> Themes { get; }
}
