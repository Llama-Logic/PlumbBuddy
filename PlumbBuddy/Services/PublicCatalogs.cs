namespace PlumbBuddy.Services;

public class PublicCatalogs :
    IPublicCatalogs
{
    public PublicCatalogs(ILogger<PublicCatalogs> logger, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        this.logger = logger;
        this.settings = settings;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        if (this.settings.UsePublicPackCatalog)
            Task.Run(() => FetchPackCatalogAsync());
        client = new() { BaseAddress = new("https://plumbbuddy.app/community-data/") };
    }

    readonly HttpClient client;
    readonly ILogger<PublicCatalogs> logger;
    IReadOnlyDictionary<string, PackDescription>? packCatalog;
    readonly ISettings settings;

    ~PublicCatalogs() =>
        Dispose(false);

    public IReadOnlyDictionary<string, PackDescription>? PackCatalog
    {
        get => packCatalog;
        private set
        {
            packCatalog = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan? SupportDiscordsCacheTTL
    {
        get
        {
            var supportDiscordsCachedFile = new FileInfo(Path.Combine(FileSystem.AppDataDirectory, "support-discords.yml"));
            if (!supportDiscordsCachedFile.Exists)
                return null;
            return TimeSpan.FromDays(1) - (DateTime.UtcNow - supportDiscordsCachedFile.LastWriteTimeUtc);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            client.Dispose();
        }
    }

    async Task FetchPackCatalogAsync(bool force = false)
    {
        try
        {
            var packsCachedFile = new FileInfo(Path.Combine(FileSystem.AppDataDirectory, "packs.yml"));
            if (!force && packsCachedFile.Exists && packsCachedFile.LastWriteTimeUtc.AddDays(7) > DateTime.UtcNow)
                PackCatalog = Yaml.CreateYamlDeserializer().Deserialize<Dictionary<string, PackDescription>>(await File.ReadAllTextAsync(packsCachedFile.FullName).ConfigureAwait(false));
            else
            {
                var responseMessage = await client.GetAsync("packs.yml").ConfigureAwait(false);
                responseMessage.EnsureSuccessStatusCode();
                var packsYaml = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                var packs = Yaml.CreateYamlDeserializer().Deserialize<Dictionary<string, PackDescription>>(packsYaml);
                using var packsCachedFileStream = File.Open(packsCachedFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                using var packsCachedFileStreamWriter = new StreamWriter(packsCachedFileStream);
                await packsCachedFileStreamWriter.WriteAsync(packsYaml).ConfigureAwait(false);
                await packsCachedFileStreamWriter.FlushAsync();
                PackCatalog = packs;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch pack catalog with force = {Force}", force);
            settings.UsePublicPackCatalog = false;
        }
    }

    public async Task<IReadOnlyDictionary<string, SupportDiscord>> GetSupportDiscordsAsync(bool? useCache = null)
    {
        var supportDiscordsCachedFile = new FileInfo(Path.Combine(FileSystem.AppDataDirectory, "support-discords.yml"));
        if (useCache is null or true && supportDiscordsCachedFile.Exists && supportDiscordsCachedFile.LastWriteTimeUtc.AddDays(1) > DateTime.UtcNow)
            return Yaml.CreateYamlDeserializer().Deserialize<Dictionary<string, SupportDiscord>>(await File.ReadAllTextAsync(supportDiscordsCachedFile.FullName).ConfigureAwait(false));
        else if (useCache is null or false)
        {
            var responseMessage = await client.GetAsync("support-discords.yml").ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            var supportDiscordsYaml = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            var supportDiscords = Yaml.CreateYamlDeserializer().Deserialize<Dictionary<string, SupportDiscord>>(supportDiscordsYaml);
            using var supportDiscordsCachedFileStream = File.Open(supportDiscordsCachedFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
            using var supportDiscordsCachedFileStreamWriter = new StreamWriter(supportDiscordsCachedFileStream);
            await supportDiscordsCachedFileStreamWriter.WriteAsync(supportDiscordsYaml).ConfigureAwait(false);
            await supportDiscordsCachedFileStreamWriter.FlushAsync();
            return supportDiscords;
        }
        throw new FileNotFoundException();
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.UsePublicPackCatalog))
        {
            if (settings.UsePublicPackCatalog && PackCatalog is null)
                Task.Run(() => FetchPackCatalogAsync(true));
            else
                PackCatalog = null;
        }
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
