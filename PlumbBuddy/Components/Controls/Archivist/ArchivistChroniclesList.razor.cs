namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistChroniclesList
{
    bool IncludeChronicle(Chronicle chronicle)
    {
        var chroniclesSearchText = Archivist.ChroniclesSearchText;
        if (string.IsNullOrWhiteSpace(chroniclesSearchText))
            return true;
        if (chronicle.Name.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (chronicle.Notes?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        foreach (var snapshot in chronicle.Snapshots)
        {
            if (snapshot.ActiveHouseholdName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.Label.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (snapshot.LastPlayedLotName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.LastPlayedWorldName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.LastWriteTime.ToString("g").Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (snapshot.Notes?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if ((snapshot.WasLive ? "Live" : string.Empty).Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    protected override void ConfigureBindings()
    {
        base.ConfigureBindings();
        Observed(() => Archivist.ChroniclesSearchText);
    }
}
