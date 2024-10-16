namespace PlumbBuddy.Components.Controls;

partial class ModComponentEditor
{
    bool exclusivityGuidanceOpen;
    ModComponent? lastModComponent;
    bool requirementIdentifierGuidanceOpen;

    [Parameter]
    public string? Exclusivity { get; set; }

    [Parameter]
    public FileInfo? File { get; set; }

    [Parameter]
    public string? IgnoreIfHashAvailable { get; set; }

    [Parameter]
    public string? IgnoreIfHashUnavailable { get; set; }

    [Parameter]
    public string? IgnoreIfPackAvailable { get; set; }

    KeyValuePair<string, PackDescription>? IgnoreIfPackAvailablePair
    {
        get =>
            IgnoreIfPackAvailable is { } packCode
            && (PublicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDescription) ?? false)
            ? new(packCode, packDescription)
            : null;
        set =>
            HandleIgnoreIfPackAvailableChanged(value?.Key);
    }

    [Parameter]
    public string? IgnoreIfPackUnavailable { get; set; }

    KeyValuePair<string, PackDescription>? IgnoreIfPackUnavailablePair
    {
        get =>
            IgnoreIfPackUnavailable is { } packCode
            && (PublicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDescription) ?? false)
            ? new(packCode, packDescription)
            : null;
        set =>
            HandleIgnoreIfPackUnavailableChanged(value?.Key);
    }

    [Parameter]
    public bool IsRequired { get; set; }

    [Parameter]
    public ModComponent? ModComponent { get; set; }

    [Parameter]
    public string? RequirementIdentifier { get; set; }

    public void CloseGuidance()
    {
        requirementIdentifierGuidanceOpen = false;
        exclusivityGuidanceOpen = false;
    }

    public void Dispose()
    {
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
    }

    void HandleExclusivityChanged(string? newValue)
    {
        Exclusivity = newValue;
        if (ModComponent is { } modComponent)
            modComponent.Exclusivity = newValue;
    }

    void HandleFileChanged(FileInfo? newValue)
    {
        File = newValue;
        if (ModComponent is { } modComponent && newValue is { } nonNullNewValue)
            modComponent.File = nonNullNewValue;
    }

    void HandleIgnoreIfHashAvailableChanged(string? newValue)
    {
        IgnoreIfHashAvailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfHashAvailable = newValue;
    }

    void HandleIgnoreIfHashUnavailableChanged(string? newValue)
    {
        IgnoreIfHashUnavailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfHashUnavailable = newValue;
    }

    void HandleIgnoreIfPackAvailableChanged(string? newValue)
    {
        IgnoreIfPackAvailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfPackAvailable = newValue;
    }

    void HandleIgnoreIfPackUnavailableChanged(string? newValue)
    {
        IgnoreIfPackUnavailable = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IgnoreIfPackUnavailable = newValue;
    }

    void HandleIsRequiredChanged(bool newValue)
    {
        IsRequired = newValue;
        if (ModComponent is { } modComponent)
            modComponent.IsRequired = newValue;
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.UsePublicPackCatalog))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(StateHasChanged);
            else
                StateHasChanged();
        }
    }

    void HandlePublicCatalogsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPublicCatalogs.PackCatalog))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(StateHasChanged);
            else
                StateHasChanged();
        }
    }

    void HandleRequirementIdentifierChanged(string? newValue)
    {
        RequirementIdentifier = newValue;
        if (ModComponent is { } modComponent)
            modComponent.RequirementIdentifier = newValue;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModComponent != lastModComponent)
        {
            lastModComponent = ModComponent;
            await SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(Exclusivity), lastModComponent?.Exclusivity },
                { nameof(File), lastModComponent?.File },
                { nameof(IgnoreIfPackAvailable), lastModComponent?.IgnoreIfPackAvailable },
                { nameof(IgnoreIfPackUnavailable), lastModComponent?.IgnoreIfPackUnavailable },
                { nameof(IgnoreIfHashAvailable), lastModComponent?.IgnoreIfHashAvailable },
                { nameof(IgnoreIfHashUnavailable), lastModComponent?.IgnoreIfHashUnavailable },
                { nameof(IsRequired), lastModComponent?.IsRequired },
                { nameof(RequirementIdentifier), lastModComponent?.RequirementIdentifier }
            }));
        }
    }
}
