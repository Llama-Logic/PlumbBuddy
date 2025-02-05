namespace PlumbBuddy.Services;

public interface IParlay :
    IDisposable,
    INotifyPropertyChanged
{
    string EntrySearchText { get; set; }

    IReadOnlyList<ParlayPackage> Packages { get; }

    ParlayPackage? SelectedPackage { get; set; }

    ParlayStringTable? ShownStringTable { get; set; }

    ReadOnlyObservableCollection<ParlayStringTableEntry>? StringTableEntries { get; }

    ParlayLocale? TranslationLocale { get; set; }

    ImmutableArray<ParlayLocale> TranslationLocales { get; }

    void SaveTranslation() =>
        _ = Task.Run(SaveTranslationAsync);

    Task SaveTranslationAsync();
}
