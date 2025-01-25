namespace PlumbBuddy.Services;

public interface ICatalog :
    IDisposable,
    INotifyPropertyChanged
{
    IReadOnlyDictionary<CatalogModKey, IReadOnlyList<(ModFileManifestModel manifest, IReadOnlyList<FileInfo> files, IReadOnlyList<CatalogModKey> dependencies, IReadOnlyList<CatalogModKey> dependents)>> Mods { get; set; }

    string ModsSearchText { get; set; }

    CatalogModKey? SelectedModKey { get; set; }
}
