namespace PlumbBuddy.Services.Translation;

public interface IParlay :
    IDisposable,
    INotifyPropertyChanged
{
    string? Creators { get; }

    ParlayStringTableEntry? EditingEntry { get; set; }

    string EditingEntryTranslation { get; set; }

    string EntrySearchText { get; set; }

    string? MessageFromCreators { get; }

    FileInfo? OriginalPackageFile { get; set; }

    IReadOnlyList<ParlayPackage> Packages { get; }

    ParlayPackage? SelectedPackage { get; set; }

    ParlayStringTable? ShownStringTable { get; set; }

    ReadOnlyObservableCollection<ParlayStringTableEntry>? StringTableEntries { get; }

    ParlayLocale? TranslationLocale { get; set; }

    ImmutableArray<ParlayLocale> TranslationLocales { get; }

    FileInfo? TranslationPackageFile { get; set; }

    Uri? TranslationSubmissionUrl { get; }

    Task<int> MergeStringTableAsync(FileInfo fromPackageFile);

    void SaveTranslation() =>
        _ = Task.Run(SaveTranslationAsync);

    Task SaveTranslationAsync();
}
