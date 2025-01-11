namespace PlumbBuddy.Services;

public interface IArchivist :
    IDisposable,
    INotifyPropertyChanged
{
    string ChroniclesSearchText { get; set; }

    Chronicle? SelectedChronicle { get; set; }

    string SnapshotsSearchText { get; set; }

    ArchivistState State { get; }

    ReadOnlyObservableCollection<Chronicle> Chronicles { get; }

    Task AddPathToProcessAsync(FileSystemInfo fileSystemInfo);

    Task ReapplyEnhancementsAsync(Chronicle chronicle);
}
