namespace PlumbBuddy.Services;

public interface IPublicCatalogs :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyDictionary<string, PackDescription>? PackCatalog { get; }

    TimeSpan? SupportDiscordsCacheTTL { get; }

    Task<IReadOnlyDictionary<string, SupportDiscord>> GetSupportDiscordsAsync(bool? useCache = null);
}
