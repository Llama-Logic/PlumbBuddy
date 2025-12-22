namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistChronicleDisplay
{
    readonly CollectionObserver collectionObserver = new();
    IObservableCollectionQuery<Snapshot>? snapshots;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Archivist.PropertyChanged -= HandleArchivistPropertyChanged;
            snapshots?.Dispose();
        }
    }

    void HandleArchivistPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IArchivist.SelectedChronicle))
            ResetSnapshotsQuery();
    }

    bool IncludeSnapshot(Snapshot snapshot)
    {
        var snapshotsTextSearch = Archivist.SnapshotsSearchText;
        if (string.IsNullOrWhiteSpace(snapshotsTextSearch))
            return true;
        if (snapshot.ActiveHouseholdName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.Label.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        if (snapshot.LastPlayedLotName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.LastPlayedWorldName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.LastWriteTime.ToString("g").Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        if (snapshot.Notes?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if ((snapshot.WasLive ? "Live" : string.Empty).Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Archivist.PropertyChanged += HandleArchivistPropertyChanged;
        ResetSnapshotsQuery();
    }

    void ResetSnapshotsQuery()
    {
        snapshots?.Dispose();
        if (Archivist.SelectedChronicle is { } chronicle)
            snapshots = collectionObserver.ObserveReadOnlyList(chronicle.Snapshots).ObserveUsingSynchronizationContextEventually(MainThreadDetails.SynchronizationContext);
    }
}
