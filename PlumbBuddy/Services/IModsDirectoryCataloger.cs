namespace PlumbBuddy.Services;

public interface IModsDirectoryCataloger :
    INotifyPropertyChanged
{
    bool IsUpdatingCatalog { get; }

    int PackageCount { get; }

    int ResourceCount { get; }

    int ScriptArchiveCount { get; }

    void Catalog(string path);
}
