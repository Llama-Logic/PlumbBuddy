namespace PlumbBuddy.Models;

[SuppressMessage("Naming", "CA1724: Type names should not match namespaces")]
public static class Yaml
{
    static TBuilder ConfigureBuilder<TBuilder>(TBuilder builder)
        where TBuilder : BuilderSkeleton<TBuilder> => builder
            .WithTypeConverter(new YamlHashHexConverter())
            .WithTypeConverter(new YamlUriConverter())
            .WithNamingConvention(UnderscoredNamingConvention.Instance);

    public static IDeserializer CreateYamlDeserializer() =>
        ConfigureBuilder(new DeserializerBuilder()).IgnoreUnmatchedProperties().Build();

    public static ISerializer CreateYamlSerializer() =>
        ConfigureBuilder(new SerializerBuilder()).Build();

    [SuppressMessage("Design", "CA1054: URI-like parameters should not be strings")]
    public static async Task<TValue> GetFromYamlAsync<TValue>(this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
        var responseMessage = await client.GetAsync(requestUri).ConfigureAwait(false);
        responseMessage.EnsureSuccessStatusCode();
        return CreateYamlDeserializer().Deserialize<TValue>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
    }
}
