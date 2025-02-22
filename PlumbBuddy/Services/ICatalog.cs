namespace PlumbBuddy.Services;

public interface ICatalog :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyDictionary<CatalogModKey, IReadOnlyList<CatalogModValue>> Mods { get; set; }

    string ModsSearchText { get; set; }

    CatalogModKey? SelectedModKey { get; set; }
}
