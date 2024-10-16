namespace PlumbBuddy.Services;

public interface IPublicCatalogs :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyDictionary<string, PackDescription>? PackCatalog { get; }
}
