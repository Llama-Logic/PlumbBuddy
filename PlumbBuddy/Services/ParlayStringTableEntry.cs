namespace PlumbBuddy.Services;

public class ParlayStringTableEntry :
    INotifyPropertyChanged
{
    public ParlayStringTableEntry(IParlay parlay, uint hash, string original, string translation)
    {
        ArgumentNullException.ThrowIfNull(parlay);
        ArgumentNullException.ThrowIfNull(translation);
        this.parlay = parlay;
        Hash = hash;
        Original = original;
        this.translation = translation;
    }

    readonly IParlay parlay;
    string translation;

    public uint Hash { get; }

    public string Original { get; }

    public string Translation
    {
        get => translation;
        set
        {
            if (translation == value)
                return;
            translation = value;
            OnPropertyChanged();
            parlay.SaveTranslation();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}
