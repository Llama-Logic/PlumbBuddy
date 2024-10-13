namespace PlumbBuddy.Components.Controls;

public class ModComponent(FileInfo file, bool isRequired, string? requirementIdentifier, string? ignoreIfPackAvailable, string? ignoreIfPackUnavailable, string? ignoreIfHashAvailable, string? ignoreIfHashUnavailable, string? exclusivity, string? title) :
    INotifyPropertyChanged
{
    string? exclusivity = exclusivity;
    FileInfo file = file;
    string? ignoreIfHashAvailable = ignoreIfHashAvailable;
    string? ignoreIfHashUnavailable = ignoreIfHashUnavailable;
    string? ignoreIfPackAvailable = ignoreIfPackAvailable;
    string? ignoreIfPackUnavailable = ignoreIfPackUnavailable;
    bool isRequired = isRequired;
    string? requirementIdentifier = requirementIdentifier;
    string? title = title;

    public string? Exclusivity
    {
        get => exclusivity;
        set
        {
            if (exclusivity == value)
                return;
            exclusivity = value;
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

    public string? Title
    {
        get => title;
        set
        {
            if (title == value)
                return;
            title = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
