namespace PlumbBuddy.Components.Controls;

partial class ModRequirementEditor
{
    ModRequirement? lastModRequirement;
    bool requirementIdentifierGuidanceOpen;

    [Parameter]
    public IReadOnlyList<string> Creators { get; set; } = [];

    [Parameter]
    public IReadOnlyList<string> Hashes { get; set; } = [];

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
    public ModRequirement? ModRequirement { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public IReadOnlyList<string> RequiredFeatures { get; set; } = [];

    [Parameter]
    public string? RequirementIdentifier { get; set; }

    [Parameter]
    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings", Justification = "Take it up with MudBlazor")]
    public string? Url { get; set; }

    [Parameter]
    public string? Version { get; set; }

    async Task HandleBrowseForAddModFileOnClickAsync()
    {
        await DialogService.ShowInfoDialogAsync("Please keep in mind ☝️",
            """
            Only pick another mod file in this mod if you're certain *your mod* requires it. You do not (and should not) try to make sure the player is also meeting the other mod's requirements.<br /><br />
            <iframe src="https://giphy.com/embed/QqmtxPQ9n6wXoPDkoc" width="270" height="480" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/joycelayman-marketing-coach-business-strategist-your-reminder-QqmtxPQ9n6wXoPDkoc">via GIPHY</a></p>
            """);
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(PbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            Hashes = Hashes.Concat([hash.ToHexString()]).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
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

    async Task HandleBrowseForModFileForManifestFeatureSelectionOnClickAsync()
    {
        var manifest = await ModFileSelector.SelectAModFileManifestAsync(PbDbContext, DialogService);
        if ((manifest?.Features.Count ?? 0) is 0)
        {
            await DialogService.ShowErrorDialogAsync("Whoops, this mod file has no offered features",
                """
                There are apparently no features on offer. If you didn't make a mistake picking the mod file just now, definitely clear out the required features!<br /><br />
                <iframe src="https://giphy.com/embed/l2JehQ2GitHGdVG9y" width="480" height="362" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/season-16-the-simpsons-16x9-l2JehQ2GitHGdVG9y">via GIPHY</a></p>
                """);
            return;
        }
        if (await DialogService.ShowSelectFeaturesDialogAsync(manifest!) is not { } selectedFeatures)
            return;
        RequiredFeatures = selectedFeatures;
    }

    void HandleCreatorsChanged(IReadOnlyList<string> newValue)
    {
        Creators = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.Creators = newValue;
    }

    void HandleHashesChanged(IReadOnlyList<string> newValue)
    {
        Hashes = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.Hashes = newValue;
    }

    void HandleIgnoreIfHashAvailableChanged(string? newValue)
    {
        IgnoreIfHashAvailable = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.IgnoreIfHashAvailable = newValue;
    }

    void HandleIgnoreIfHashUnavailableChanged(string? newValue)
    {
        IgnoreIfHashUnavailable = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.IgnoreIfHashUnavailable = newValue;
    }

    void HandleIgnoreIfPackAvailableChanged(string? newValue)
    {
        IgnoreIfPackAvailable = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.IgnoreIfPackAvailable = newValue;
    }

    void HandleIgnoreIfPackUnavailableChanged(string? newValue)
    {
        IgnoreIfPackUnavailable = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.IgnoreIfPackUnavailable = newValue;
    }

    void HandleNameChanged(string? newValue)
    {
        Name = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.Name = newValue;
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

    void HandleRequiredFeaturesChanged(IReadOnlyList<string> newValue)
    {
        RequiredFeatures = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.RequiredFeatures = newValue;
    }

    void HandleRequirementIdentifierChanged(string? newValue)
    {
        RequirementIdentifier = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.RequirementIdentifier = newValue;
    }

    void HandleUrlChanged(string? newValue)
    {
        Url = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.Url = newValue;
    }

    void HandleVersionChanged(string? newValue)
    {
        Version = newValue;
        if (ModRequirement is { } modRequirement)
            modRequirement.Version = newValue;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModRequirement == lastModRequirement)
            return;
        lastModRequirement = ModRequirement;
        await SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(Creators), (lastModRequirement?.Creators ?? []).ToList().AsReadOnly() },
            { nameof(Hashes), (lastModRequirement?.Hashes ?? []).ToList().AsReadOnly() },
            { nameof(IgnoreIfPackAvailable), lastModRequirement?.IgnoreIfPackAvailable },
            { nameof(IgnoreIfPackUnavailable), lastModRequirement?.IgnoreIfPackUnavailable },
            { nameof(IgnoreIfHashAvailable), lastModRequirement?.IgnoreIfHashAvailable },
            { nameof(IgnoreIfHashUnavailable), lastModRequirement?.IgnoreIfHashUnavailable },
            { nameof(Name), lastModRequirement?.Name },
            { nameof(RequiredFeatures), (lastModRequirement?.RequiredFeatures ?? []).ToList().AsReadOnly() },
            { nameof(RequirementIdentifier), lastModRequirement?.RequirementIdentifier },
            { nameof(Url), lastModRequirement?.Url },
            { nameof(Version), lastModRequirement?.Version }
        }));
    }
}
