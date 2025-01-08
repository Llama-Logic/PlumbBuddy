namespace PlumbBuddy.Services;

public interface IArchivist :
    IDisposable,
    INotifyPropertyChanged
{
    bool CanSafelyUpdateSaveGameData { get; }

    Chronicle? SelectedChronicle { get; set; }

    ArchivistState State { get; }

    ReadOnlyObservableCollection<Chronicle> Chronicles { get; }

    Task AddPathToProcessAsync(FileSystemInfo fileSystemInfo);

    Task ReapplyEnhancementsAsync(Chronicle chronicle);
}
