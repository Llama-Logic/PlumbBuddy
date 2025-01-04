namespace PlumbBuddy.Services;

public interface IArchivist :
    IDisposable,
    INotifyPropertyChanged
{
    bool CanIngest { get; }

    Chronicle? SelectedChronicle { get; set; }

    ArchivistState State { get; }

    ReadOnlyObservableCollection<Chronicle> Chronicles { get; }
}
