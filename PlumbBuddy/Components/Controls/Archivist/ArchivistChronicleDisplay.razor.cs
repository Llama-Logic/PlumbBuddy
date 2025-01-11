namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistChronicleDisplay
{

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
}
