namespace PlumbBuddy.Components.Controls;

[SuppressMessage("Design", "CA1054: URI-like parameters should not be strings", Justification = "Take it up with MudBlazor")]
public class ModRequirement(string? name, string hashes, string requiredFeatures, string? requirementIdentifier, string? ignoreIfPackAvailable, string? ignoreIfPackUnavailable, string? ignoreIfHashAvailable, string? ignoreIfHashUnavailable, string creators, string? url, string? version) :
    INotifyPropertyChanged
{
    string creators = creators;
    string hashes = hashes;
    string? ignoreIfHashAvailable = ignoreIfHashAvailable;
    string? ignoreIfHashUnavailable = ignoreIfHashUnavailable;
    string? ignoreIfPackAvailable = ignoreIfPackAvailable;
    string? ignoreIfPackUnavailable = ignoreIfPackUnavailable;
    string? name = name;
    string requiredFeatures = requiredFeatures;
    string? requirementIdentifier = requirementIdentifier;
    string? url = url;
    string? version = version;

    public ChipSetField? HashesField;
    public ChipSetField? RequiredFeaturesField;

    public IReadOnlyList<string> Creators
    {
        get => creators.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        set
        {
            creators = string.Join(Environment.NewLine, value);
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> Hashes
    {
        get => hashes.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        set
        {
            hashes = string.Join(Environment.NewLine, value);
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
        get => requiredFeatures.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        set
        {
            requiredFeatures = string.Join(Environment.NewLine, value);
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

    public string? Version
    {
        get => version;
        set
        {
            if (version == value)
                return;
            version = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task CommitPendingEntriesIfEmptyAsync()
    {
        if (HashesField is not null)
            await HashesField.CommitPendingEntryIfEmptyAsync();
        if (RequiredFeaturesField is not null)
            await RequiredFeaturesField.CommitPendingEntryIfEmptyAsync();
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
