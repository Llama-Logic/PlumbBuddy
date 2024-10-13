namespace PlumbBuddy.Components.Controls;

[SuppressMessage("Design", "CA1054: URI-like parameters should not be strings", Justification = "Take it up with MudBlazor")]
public class ModRequirement(string? name, IReadOnlyList<string> hashes, IReadOnlyList<string> requiredFeatures, string? requirementIdentifier, string? ignoreIfPackAvailable, string? ignoreIfPackUnavailable, string? ignoreIfHashAvailable, string? ignoreIfHashUnavailable, IReadOnlyList<string> creators, string? url, Version? version) :
    INotifyPropertyChanged
{
    IReadOnlyList<string> creators = creators;
    IReadOnlyList<string> hashes = hashes;
    string? ignoreIfHashAvailable = ignoreIfHashAvailable;
    string? ignoreIfHashUnavailable = ignoreIfHashUnavailable;
    string? ignoreIfPackAvailable = ignoreIfPackAvailable;
    string? ignoreIfPackUnavailable = ignoreIfPackUnavailable;
    string? name = name;
    IReadOnlyList<string> requiredFeatures = requiredFeatures;
    string? requirementIdentifier = requirementIdentifier;
    string? url = url;
    Version? version = version;

    public ChipSetField? CreatorsChipSetField;
    public ChipSetField? HashesChipSetField;

    public IReadOnlyList<string> Creators
    {
        get => creators;
        set
        {
            if (!creators.SequenceEqual(value))
                return;
            creators = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> Hashes
    {
        get => hashes;
        set
        {
            if (!hashes.SequenceEqual(value))
                return;
            hashes = value;
            OnPropertyChanged();
        }
    }

    public string? IgnoreIfHashAvailable
    {
        get => ignoreIfHashAvailable;
        set
        {
            if (ignoreIfHashAvailable == value)
                return;
            ignoreIfHashAvailable = value;
            OnPropertyChanged();
        }
    }

    public string? IgnoreIfHashUnavailable
    {
        get => ignoreIfHashUnavailable;
        set
        {
            if (ignoreIfHashUnavailable == value)
                return;
            ignoreIfHashUnavailable = value;
            OnPropertyChanged();
        }
    }

    public string? IgnoreIfPackAvailable
    {
        get => ignoreIfPackAvailable;
        set
        {
            if (ignoreIfPackAvailable == value)
                return;
            ignoreIfPackAvailable = value;
            OnPropertyChanged();
        }
    }

    public string? IgnoreIfPackUnavailable
    {
        get => ignoreIfPackUnavailable;
        set
        {
            if (ignoreIfPackUnavailable == value)
                return;
            ignoreIfPackUnavailable = value;
            OnPropertyChanged();
        }
    }

    public string? Name
    {
        get => name;
        set
        {
            if (name == value)
                return;
            name = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> RequiredFeatures
    {
        get => requiredFeatures;
        set
        {
            if (!requiredFeatures.SequenceEqual(value))
                return;
            requiredFeatures = value;
            OnPropertyChanged();
        }
    }

    public string? RequirementIdentifier
    {
        get => requirementIdentifier;
        set
        {
            if (requirementIdentifier == value)
                return;
            requirementIdentifier = value;
            OnPropertyChanged();
        }
    }

    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings", Justification = "Take it up with MudBlazor")]
    public string? Url
    {
        get => url;
        set
        {
            if (url == value)
                return;
            url = value;
            OnPropertyChanged();
        }
    }

    public Version? Version
    {
        get => version;
        set
        {
            if (EqualityComparer<Version>.Default.Equals(version, value))
                return;
            version = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
