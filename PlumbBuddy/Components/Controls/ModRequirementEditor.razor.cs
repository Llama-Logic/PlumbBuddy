namespace PlumbBuddy.Components.Controls;

partial class ModRequirementEditor
{
    ModRequirement? lastModRequirement;

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
        await DialogService.ShowInfoDialogAsync(AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Caption, AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Text);
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var manifest = await ModFileSelector.SelectAModFileManifestAsync(pbDbContext, DialogService);
        if (manifest is not null && !manifest.Hash.IsDefaultOrEmpty)
            HandleHashesChanged
            (
                Hashes
                    .Except(manifest.SubsumedHashes.Select(sh => sh.ToHexString()), StringComparer.OrdinalIgnoreCase)
                    .Concat([manifest.Hash.ToHexString()])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly()
            );
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

    async Task HandleBrowseForModFileForManifestFeatureSelectionOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var manifest = await ModFileSelector.SelectAModFileManifestAsync(pbDbContext, DialogService);
        if (manifest is null)
            return;
        if (manifest.Features.Count is 0)
        {
            await DialogService.ShowErrorDialogAsync(AppText.ModRequirementEditor_Error_NoManifestedFeatures_Caption, AppText.ModRequirementEditor_Error_NoManifestedFeatures_Text);
            return;
        }
        if (await DialogService.ShowSelectFeaturesDialogAsync(manifest!) is not { } selectedFeatures)
            return;
        HandleRequiredFeaturesChanged(selectedFeatures);
    }

    async Task HandleDropAModFileForAddedHashOnClickAsync()
    {
        await DialogService.ShowInfoDialogAsync(AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Caption, AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Text);
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var manifest = await ModFileSelector.GetADroppedModFileManifestAsync(UserInterfaceMessaging, pbDbContext, DialogService);
        if (manifest is not null && !manifest.Hash.IsDefaultOrEmpty)
            HandleHashesChanged
            (
                Hashes
                    .Except(manifest.SubsumedHashes.Select(sh => sh.ToHexString()), StringComparer.OrdinalIgnoreCase)
                    .Concat([manifest.Hash.ToHexString()])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly()
            );
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

    async Task HandleDropAModFileForManifestFeatureSelectionOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var manifest = await ModFileSelector.GetADroppedModFileManifestAsync(UserInterfaceMessaging, pbDbContext, DialogService);
        if (manifest is null)
            return;
        if (manifest.Features.Count is 0)
        {
            await DialogService.ShowErrorDialogAsync(AppText.ModRequirementEditor_Error_NoManifestedFeatures_Caption, AppText.ModRequirementEditor_Error_NoManifestedFeatures_Text);
            return;
        }
        if (await DialogService.ShowSelectFeaturesDialogAsync(manifest!) is not { } selectedFeatures)
            return;
        HandleRequiredFeaturesChanged(selectedFeatures);
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

    async Task HandleSelectCatalogedModFileForAddedHashOnClickAsync()
    {
        await DialogService.ShowInfoDialogAsync(AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Caption, AppText.ModRequirementEditor_Info_AddHashesRequirementDomainReminder_Text);
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await DialogService.ShowSelectCatalogedModFileDialogAsync() is { } modFilePath
            && await ModFileSelector.GetSelectedModFileManifestAsync(pbDbContext, DialogService, new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", modFilePath))) is { } manifest
            && !manifest.Hash.IsDefaultOrEmpty)
            HandleHashesChanged
            (
                Hashes
                    .Except(manifest.SubsumedHashes.Select(sh => sh.ToHexString()), StringComparer.OrdinalIgnoreCase)
                    .Concat([manifest.Hash.ToHexString()])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly()
            );
    }

    async Task HandleSelectCatalogedModFileForManifestFeatureSelectionOnClickAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await DialogService.ShowSelectCatalogedModFileDialogAsync() is not { } modFilePath
            || await ModFileSelector.GetSelectedModFileManifestAsync(pbDbContext, DialogService, new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", modFilePath))) is not { } manifest)
            return;
        if (manifest.Features.Count is 0)
        {
            await DialogService.ShowErrorDialogAsync(AppText.ModRequirementEditor_Error_NoManifestedFeatures_Caption, AppText.ModRequirementEditor_Error_NoManifestedFeatures_Text);
            return;
        }
        if (await DialogService.ShowSelectFeaturesDialogAsync(manifest!) is not { } selectedFeatures)
            return;
        HandleRequiredFeaturesChanged(selectedFeatures);
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
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ModRequirement == lastModRequirement)
            return;
        var newModRequirement = ModRequirement;
        ModRequirement = lastModRequirement;
        if (ModRequirement is { } modRequirement)
            await modRequirement.CommitPendingEntriesIfEmptyAsync();
        ModRequirement = newModRequirement;
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
