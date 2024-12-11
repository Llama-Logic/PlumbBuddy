namespace PlumbBuddy.Components.Controls;

partial class ModComponentEditor
{
    ChipSetField? exclusivitiesField;
    ModComponent? lastModComponent;
    ChipSetField? subsumedHashesField;

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

    public async Task CommitPendingEntriesIfEmptyAsync()
    {
        if (ModComponent is not null)
        {
            if (exclusivitiesField is not null)
                await exclusivitiesField.CommitPendingEntryIfEmptyAsync();
            if (subsumedHashesField is not null)
                await subsumedHashesField.CommitPendingEntryIfEmptyAsync();
        }
    }

    public void Dispose()
    {
        Settings.PropertyChanged -= HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
    }

    async Task HandleBrowseForAddSubsumedHashOnClickAsync()
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.ManifestComponentEditor_Caution_AddSubsumedHash_Caption, AppText.ManifestComponentEditor_Caution_AddSubsumedHash_Text))
            return;
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(pbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleSubsumedHashesChanged(SubsumedHashes.Concat([hash.ToHexString()]).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly());
    }

    async Task HandleBrowseForIgnoreIfHashAvailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(pbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleIgnoreIfHashAvailableChanged(hash.ToHexString());
    }

    async Task HandleBrowseForIgnoreIfHashUnavailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.SelectAModFileManifestHashAsync(pbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleIgnoreIfHashUnavailableChanged(hash.ToHexString());
    }

    async Task HandleDropAnForAddSubsumedHashOnClickAsync()
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.ManifestComponentEditor_Caution_AddSubsumedHash_Caption, AppText.ManifestComponentEditor_Caution_AddSubsumedHash_Text))
            return;
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.GetADroppedModFileManifestHashAsync(UserInterfaceMessaging, pbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleSubsumedHashesChanged(SubsumedHashes.Concat([hash.ToHexString()]).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly());
    }

    async Task HandleDropAnIgnoreIfHashAvailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.GetADroppedModFileManifestHashAsync(UserInterfaceMessaging, pbDbContext, DialogService);
        if (!hash.IsDefaultOrEmpty)
            HandleIgnoreIfHashAvailableChanged(hash.ToHexString());
    }

    async Task HandleDropAnIgnoreIfHashUnavailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var hash = await ModFileSelector.GetADroppedModFileManifestHashAsync(UserInterfaceMessaging, pbDbContext, DialogService);
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

    async Task HandleSelectCatalogedIgnoreIfHashAvailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await DialogService.ShowSelectCatalogedModFileDialogAsync() is { } modFilePath
            && await ModFileSelector.GetSelectedModFileManifestAsync(pbDbContext, DialogService, new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", modFilePath))) is { } manifest
            && manifest.Hash is { IsDefaultOrEmpty: false } hash)
            HandleIgnoreIfHashAvailableChanged(hash.ToHexString());
    }

    async Task HandleSelectCatalogedIgnoreIfHashUnavailableModFileOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await DialogService.ShowSelectCatalogedModFileDialogAsync() is { } modFilePath
            && await ModFileSelector.GetSelectedModFileManifestAsync(pbDbContext, DialogService, new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", modFilePath))) is { } manifest
            && manifest.Hash is { IsDefaultOrEmpty: false } hash)
            HandleIgnoreIfHashUnavailableChanged(hash.ToHexString());
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.UsePublicPackCatalog))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandlePublicCatalogsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPublicCatalogs.PackCatalog))
            StaticDispatcher.Dispatch(StateHasChanged);
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
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModComponent == lastModComponent)
            return;
        var newModComponent = ModComponent;
        ModComponent = lastModComponent;
        await CommitPendingEntriesIfEmptyAsync();
        ModComponent = newModComponent;
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
