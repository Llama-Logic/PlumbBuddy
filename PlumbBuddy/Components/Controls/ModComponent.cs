namespace PlumbBuddy.Components.Controls;

[SuppressMessage("Design", "CA1054: URI-like parameters should not be strings")]
public class ModComponent(bool isManifestPreExisting, FileInfo file, IDisposable fileObjectModel, string? manifestResourceName, bool isRequired, string? requirementIdentifier, string? ignoreIfPackAvailable, string? ignoreIfPackUnavailable, string? ignoreIfHashAvailable, string? ignoreIfHashUnavailable, string exclusivities, string? messageToTranslators, string? translationSubmissionUrl, string translators, string? name, string subsumedHashes) :
    IDisposable,
    INotifyPropertyChanged
{
    string exclusivities = exclusivities;
    FileInfo file = file;
    string? ignoreIfHashAvailable = ignoreIfHashAvailable;
    string? ignoreIfHashUnavailable = ignoreIfHashUnavailable;
    string? ignoreIfPackAvailable = ignoreIfPackAvailable;
    string? ignoreIfPackUnavailable = ignoreIfPackUnavailable;
    bool isRequired = isRequired;
    string? manifestResourceName = manifestResourceName;
    string? messageToTranslators = messageToTranslators;
    string? name = name;
    string? requirementIdentifier = requirementIdentifier;
    string subsumedHashes = subsumedHashes;
    string? translationSubmissionUrl = translationSubmissionUrl;
    string translators = translators;

    public IReadOnlyList<string> Exclusivities
    {
        get => exclusivities.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        set
        {
            exclusivities = string.Join(Environment.NewLine, value);
            OnPropertyChanged();
        }
    }

    public FileInfo File
    {
        get => file;
        set
        {
            if (file == value)
                return;
            file = value;
            OnPropertyChanged();
        }
    }

    public IDisposable FileObjectModel { get; } = fileObjectModel;

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

    public bool IsManifestPreExisting { get; } = isManifestPreExisting;

    public bool IsRequired
    {
        get => isRequired;
        set
        {
            if (isRequired == value)
                return;
            isRequired = value;
            OnPropertyChanged();
        }
    }

    public string? ManifestResourceName
    {
        get => manifestResourceName;
        set
        {
            if (manifestResourceName == value)
                return;
            manifestResourceName = value;
            OnPropertyChanged();
        }
    }

    public string? MessageToTranslators
    {
        get => messageToTranslators;
        set
        {
            if (messageToTranslators == value)
                return;
            messageToTranslators = value;
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

    public IReadOnlyList<string> SubsumedHashes
    {
        get => subsumedHashes.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        set
        {
            subsumedHashes = string.Join(Environment.NewLine, value);
            OnPropertyChanged();
        }
    }

    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings")]
    public string? TranslationSubmissionUrl
    {
        get => translationSubmissionUrl;
        set
        {
            if (translationSubmissionUrl == value)
                return;
            translationSubmissionUrl = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<(string name, CultureInfo language)> Translators
    {
        get
        {
            if (string.IsNullOrWhiteSpace(translators))
                return [];
            var split = translators.Split(Environment.NewLine);
            if (split.Length % 2 != 0)
                throw new Exception("ack");
            return Enumerable
                .Range(0, split.Length / 2)
                .Select(i => (split[i * 2], CultureInfo.GetCultureInfo(split[i * 2 + 1])))
                .ToList()
                .AsReadOnly();
        }
        set
        {
            translators = string.Join(Environment.NewLine, value.Select(t => $"{t.name}{Environment.NewLine}{t.language.Name}"));
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() =>
        FileObjectModel.Dispose();

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
