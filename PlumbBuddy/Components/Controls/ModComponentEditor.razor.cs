namespace PlumbBuddy.Components.Controls;

partial class ModComponentEditor
{
    ModComponent? lastModComponent;
    bool requirementIdentifierGuidanceOpen;

    [Parameter]
    public IReadOnlyList<string> Exclusivities { get; set; } = [];

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
    public string? ManifestResourceName { get; set; }

    [Parameter]
    public ModComponent? ModComponent { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? RequirementIdentifier { get; set; }

    [Parameter]
    public IReadOnlyList<string> SubsumedHashes { get; set; } = [];

    public void CloseGuidance() =>
        requirementIdentifierGuidanceOpen = false;

    public void Dispose()
    {
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
    }

    async Task HandleBrowseForAddSubsumedHashOnClickAsync()
    {
        if (!await DialogService.ShowCautionDialogAsync("Tread lightly here 🤚",
            """
            What you're *about to do* is tell me that this mod file counts for the one you're about to select in the file picker. Be **sure** you understand the ramifications of that before you do it.
            <iframe src="https://giphy.com/embed/4DvP1HK8UviaOuRcCY" width="480" height="480" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/SPTV-be-careful-cordell-walker-texas-ranger-4DvP1HK8UviaOuRcCY">via GIPHY</a></p>
            """))
            return;
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(PbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            SubsumedHashes = SubsumedHashes.Concat([hash.ToHexString()]).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
    }

    async Task HandleBrowseForIgnoreIfHashAvailableModFileOnClickAsync()
    {
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(PbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleIgnoreIfHashAvailableChanged(hash.ToHexString());
    }

    async Task HandleBrowseForIgnoreIfHashUnavailableModFileOnClickAsync()
    {
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(PbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleIgnoreIfHashUnavailableChanged(hash.ToHexString());
    }

    void HandleExclusivitiesChanged(IReadOnlyList<string> newValue)
    {
        Exclusivities = newValue;
        if (ModComponent is { } modComponent)
            modComponent.Exclusivities = newValue;
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

    void HandleManifestResourceNameChanged(string? newValue)
    {
        ManifestResourceName = newValue;
        if (ModComponent is { } modComponent)
            modComponent.ManifestResourceName = newValue;
    }

    void HandleNameChanged(string? newValue)
    {
        Name = newValue;
        if (ModComponent is { } modComponent)
            modComponent.Name = newValue;
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

    void HandleSubsumedHashesChanged(IReadOnlyList<string> newValue)
    {
        SubsumedHashes = newValue;
        if (ModComponent is { } modComponent)
            modComponent.SubsumedHashes = newValue;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModComponent == lastModComponent)
            return;
        lastModComponent = ModComponent;
        await SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(Exclusivities), (lastModComponent?.Exclusivities ?? []).ToList().AsReadOnly() },
            { nameof(File), lastModComponent?.File },
            { nameof(IgnoreIfPackAvailable), lastModComponent?.IgnoreIfPackAvailable },
            { nameof(IgnoreIfPackUnavailable), lastModComponent?.IgnoreIfPackUnavailable },
            { nameof(IgnoreIfHashAvailable), lastModComponent?.IgnoreIfHashAvailable },
            { nameof(IgnoreIfHashUnavailable), lastModComponent?.IgnoreIfHashUnavailable },
            { nameof(IsRequired), lastModComponent?.IsRequired },
            { nameof(ManifestResourceName), lastModComponent?.ManifestResourceName },
            { nameof(Name), lastModComponent?.Name },
            { nameof(RequirementIdentifier), lastModComponent?.RequirementIdentifier },
            { nameof(SubsumedHashes), (lastModComponent?.SubsumedHashes ?? []).ToList().AsReadOnly() }
        }));
    }
}
