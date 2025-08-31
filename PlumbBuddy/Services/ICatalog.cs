namespace PlumbBuddy.Services;

public interface ICatalog :
    IDisposable,
    INotifyPropertyChanged
{
    bool IsQuerying { get; }

    IReadOnlyDictionary<CatalogModKey, IReadOnlyList<CatalogModValue>> Mods { get; set; }

    string ModsSearchText { get; set; }

    IReadOnlyDictionary<string, string> PackIcons { get; }

    CatalogModKey? SelectedModKey { get; set; }
}
