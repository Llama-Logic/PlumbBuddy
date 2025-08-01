namespace PlumbBuddy.Components.Controls;

public partial class RecommendedPack(IPublicCatalogs publicCatalogs, string packCode, string reason) :
    INotifyPropertyChanged
{
    string packCode = packCode;
    string reason = reason;

    public string PackCode
    {
        get => packCode;
        set
        {
            if (packCode == value)
                return;
            packCode = value;
            OnPropertyChanged();
        }
    }

    public KeyValuePair<string, PackDescription>? PackPair
    {
        get =>
            PackCode is { } packCode
            && (publicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDescription) ?? false)
            ? new(packCode, packDescription)
            : null;
        set =>
            packCode = value?.Key ?? string.Empty;
    }

    public string Reason
    {
        get => reason;
        set
        {
            if (reason == value)
                return;
            reason = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}
