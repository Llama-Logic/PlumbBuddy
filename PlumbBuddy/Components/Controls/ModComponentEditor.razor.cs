namespace PlumbBuddy.Components.Controls;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
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
    public string? MessageToTranslators { get; set; }

    [Parameter]
    public ModComponent? ModComponent { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? RequirementIdentifier { get; set; }

    [Parameter]
    public IReadOnlyList<string> SubsumedHashes { get; set; } = [];

    [Parameter]
    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings")]
    public string? TranslationSubmissionUrl { get; set; }

    [Parameter]
    public IReadOnlyList<(string name, CultureInfo language)> Translators { get; set; } = [];

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

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task HandleIntegrateTranslatorsOverridePackageOnClickAsync()
    {
        if (ModComponent?.FileObjectModel is not DataBasePackedFile componentDbpf
            || await ModFileSelector.SelectAModFileAsync().ConfigureAwait(false) is not { } modFile
            || !modFile.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
            return;
        DataBasePackedFile? overrideDbpf = null;
        try
        {
            overrideDbpf = await DataBasePackedFile.FromPathAsync(modFile.FullName).ConfigureAwait(false);
            var repurposedLanguages = (await ModFileManifestModel.GetModFileManifestsAsync(overrideDbpf).ConfigureAwait(false))
                .SelectMany(kv => kv.Value.RepurposedLanguages)
                .GroupBy(rl => rl.GameLocale)
                .ToImmutableDictionary(g => g.Key, g => g.First());
            foreach (var stblKey in (await overrideDbpf.GetKeysAsync().ConfigureAwait(false)).Where(k => k.Type is ResourceType.StringTable))
            {
                var stblNeutralLocale = SmartSimUtilities.GetStringTableLocale(stblKey);
                if (repurposedLanguages.TryGetValue(SmartSimUtilities.GetStringTableLocale(stblKey).Name, out var repurposedLanguage)
                    && !CultureInfo.GetCultureInfo(repurposedLanguage.ActualLocale).GetNeutralCultureInfo().Equals(stblNeutralLocale))
                {
                    await DialogService.ShowErrorDialogAsync(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_MaxisNonStandardLanguages_Caption, AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_MaxisNonStandardLanguages_Text);
                    return;
                }
            }
            foreach (var stblKey in (await overrideDbpf.GetKeysAsync().ConfigureAwait(false)).Where(k => k.Type is ResourceType.StringTable))
            {
                var stblNeutralLocale = SmartSimUtilities.GetStringTableLocale(stblKey);
                var overrideStbl = await overrideDbpf.GetModelAsync<StringTableModel>(stblKey).ConfigureAwait(false);
                if (await componentDbpf.ContainsKeyAsync(stblKey).ConfigureAwait(false))
                {
                    if (!await DialogService.ShowCautionDialogAsync(string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_Overwriting_Caption, stblKey, ModComponent.File.Name), string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_Overwriting_Text, stblKey.FullInstanceHex, stblNeutralLocale.EnglishName)))
                        return;
                    var componentStbl = await componentDbpf.GetModelAsync<StringTableModel>(stblKey).ConfigureAwait(false);
                    var missingKeyHashes = componentStbl.KeyHashes.Except(overrideStbl.KeyHashes).ToImmutableArray();
                    if (missingKeyHashes.Any()
                        && !await DialogService.ShowCautionDialogAsync(string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_MissingKeyHashes_Caption, stblNeutralLocale.EnglishName), string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_MissingKeyHashes_Text, AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_MissingKeyHashes_Text_KeyHash.ToQuantity(missingKeyHashes.Length), stblNeutralLocale.EnglishName, string.Join(Environment.NewLine, missingKeyHashes.Select(missingKeyHash => $"* `{missingKeyHash:x8}`")))))
                        return;
                    var extraKeyHashes = overrideStbl.KeyHashes.Except(componentStbl.KeyHashes).ToImmutableArray();
                    if (extraKeyHashes.Any())
                    {
                        if (await DialogService.ShowQuestionDialogAsync(string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_ExtraKeyHashes_Caption, stblNeutralLocale.EnglishName), string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_ExtraKeyHashes_Text, stblNeutralLocale.EnglishName, AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_ExtraKeyHashes_Text_ExtraKeyHash.ToQuantity(extraKeyHashes.Length), string.Join(Environment.NewLine, extraKeyHashes.Select(missingKeyHash => $"* `{missingKeyHash:x8}`"))), userCanCancel: true) is not { } includeExtraKeyHashes)
                            return;
                        if (!includeExtraKeyHashes)
                            foreach (var extraKeyHash in extraKeyHashes)
                                overrideStbl.Delete(extraKeyHash);
                    }
                }
                else if (!await DialogService.ShowCautionDialogAsync(string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_StringTableNotFound_Caption, stblKey.FullInstanceHex, ModComponent.File.Name), string.Format(AppText.ManifestComponentEditor_IntegrateTranslatorsOverridePackage_StringTableNotFound_Text, stblKey, stblNeutralLocale.EnglishName, modFile.Name, ModComponent.File.Name)))
                    continue;
                await componentDbpf.SetAsync(stblKey, overrideStbl).ConfigureAwait(false);
            }
            HandleTranslatorsChanged(Translators
                .Union
                (
                    (await ModFileManifestModel.GetModFileManifestsAsync(overrideDbpf).ConfigureAwait(false))
                        .SelectMany(manifest => manifest.Value.Translators)
                        .Select(translator => (translator.Name, new CultureInfo(translator.Language)))
                        .Distinct()
                )
                .ToList()
                .AsReadOnly());
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, ex.ToString());
        }
        finally
        {
            overrideDbpf?.Dispose();
        }
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

    void HandleMessageToTranslatorsChanged(string? newValue)
    {
        MessageToTranslators = newValue;
        if (ModComponent is { } modComponent)
            modComponent.MessageToTranslators = newValue;
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

    void HandleTranslationSubmissionUrlChanged(string? newValue)
    {
        TranslationSubmissionUrl = newValue;
        if (ModComponent is { } modComponent)
            modComponent.TranslationSubmissionUrl = newValue;
    }

    async Task HandleTranslatorChipClosedAsync(MudChip<string> chip)
    {
        if (chip.Tag is not ValueTuple<string, CultureInfo> translator)
            return;
        var (name, language) = translator;
        var languageName = language.EnglishName;
        if (languageName != language.NativeName)
            languageName = $"{languageName} [{language.NativeName}]";
        if (!await DialogService.ShowCautionDialogAsync(AppText.ManifestComponentEditor_TranslatorChipClosed_Caution_Caption, string.Format(AppText.ManifestComponentEditor_TranslatorChipClosed_Caution_Text, name, languageName)))
            return;
        HandleTranslatorsChanged(Translators
            .Except(new (string name, CultureInfo language)[] { translator })
            .ToList()
            .AsReadOnly());
    }

    void HandleTranslatorsChanged(IReadOnlyList<(string name, CultureInfo language)> newValue)
    {
        Translators = newValue;
        if (ModComponent is { } modComponent)
            modComponent.Translators = newValue;
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
            { nameof(MessageToTranslators), lastModComponent?.MessageToTranslators },
            { nameof(Name), lastModComponent?.Name },
            { nameof(RequirementIdentifier), lastModComponent?.RequirementIdentifier },
            { nameof(SubsumedHashes), (lastModComponent?.SubsumedHashes ?? []).ToList().AsReadOnly() },
            { nameof(TranslationSubmissionUrl), lastModComponent?.TranslationSubmissionUrl },
            { nameof(Translators), (lastModComponent?.Translators ?? []).ToList().AsReadOnly() }
        }));
    }
}
