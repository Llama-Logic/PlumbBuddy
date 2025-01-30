namespace PlumbBuddy.Services;

public interface IArchivist :
    IDisposable,
    INotifyPropertyChanged
{
    string ChroniclesSearchText { get; set; }

    string? DiagnosticStatus { get; }

    Chronicle? SelectedChronicle { get; set; }

    string SnapshotsSearchText { get; set; }

    ArchivistState State { get; }

    ReadOnlyObservableCollection<Chronicle> Chronicles { get; }

    Task AddPathToProcessAsync(FileSystemInfo fileSystemInfo);

    Task LoadChronicleAsync(Chronicle chronicle);

    Task ReapplyEnhancementsAsync(Chronicle chronicle);
}
