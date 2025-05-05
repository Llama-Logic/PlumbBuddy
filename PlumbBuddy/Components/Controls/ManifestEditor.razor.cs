namespace PlumbBuddy.Components.Controls;

[SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
partial class ManifestEditor
{
    enum AddFileResult
    {
        NonExistent,
        Unrecognized,
        Unreadable,
        Succeeded,
        AlreadyAdded,
        SucceededManifested
    }

    static readonly string[] hashingLevelTickMarkLabels =
    [
        AppText.ManifestEditor_Hashing_Slider_TickMark_1,
        AppText.ManifestEditor_Hashing_Slider_TickMark_2,
        AppText.ManifestEditor_Hashing_Slider_TickMark_3
    ];
    static readonly AsyncManualResetEvent compositionResetEvent = new(true);

    public static bool RequestToRemainAlive { get; private set; }

    [GeneratedRegex(@"(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex GetSemVerInModFileNamePattern();

    [GeneratedRegex(@"^https?://.*")]
    private static partial Regex GetUrlPattern();

    public static Task WaitForCompositionClearance(CancellationToken cancellationToken = default) =>
        compositionResetEvent.WaitAsync(cancellationToken);

    bool addRequiredModGuidanceOpen;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed")]
    CancellationTokenSource? batchCancellationTokenSource;
    bool batchContinue;
    string batchOverlayFilename = string.Empty;
    int batchOverlayMax = 0;
    int batchOverlayValue = 0;
    bool batchOverlayVisible;
    TaskCompletionSource<IReadOnlyList<FileInfo>>? batchTaskCompletionSource;
    DirectoryInfo? commonComponentDirectory;
    readonly List<ModComponent> components = [];
    ModComponent? componentsStepSelectedComponent;
    IReadOnlyCollection<ModComponent> componentsStepSelectedComponents = [];
    MudBlazor.SelectionMode componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
    readonly List<TreeItemData<ModComponent?>> componentTreeItemData = [];
    string compositionStatus = string.Empty;
    int compositionStep;
    IReadOnlyList<(Severity severity, string? icon, string message)> confirmationStepMessages = [];
    IReadOnlyList<string> creators = [];
    ChipSetField? creatorsChipSetField;
    string description = string.Empty;
    MudForm? detailsStepForm;
    string electronicArtsPromoCode = string.Empty;
    IReadOnlyList<string> features = [];
    ChipSetField? featuresChipSetField;
    int hashingLevel = 1;
    bool isComposing;
    bool isLoading;
    IReadOnlyList<string> incompatiblePacks = [];
    ChipSetField? incompatibleChipSetField;
    string loadingText = string.Empty;
    ModComponentEditor? modComponentEditor;
    string name = string.Empty;
    string originalVersion = string.Empty;
    readonly List<ModRequirement> requiredMods = [];
    IReadOnlyList<string> requiredPacks = [];
    ChipSetField? requiredPacksChipSetField;
    MudForm? requirementsStepForm;
    MudStepperExtended? stepper;
    FileInfo? selectStepFile;
    ModsDirectoryFileType selectStepFileType;
    MudForm? selectStepForm;
    string url = string.Empty;
    bool urlEncouragingGuidanceOpen;
    bool urlProhibitiveGuidanceOpen;
    string version = string.Empty;
    bool versionEnabled;

    ModComponent? ComponentsStepSelectedComponent
    {
        get => componentsStepSelectedComponent;
        set
        {
            if (componentsStepSelectedComponent == value)
                return;
            componentsStepSelectedComponent = value;
            StateHasChanged();
        }
    }

    IEnumerable<KeyValuePair<string, PackDescription>> IncompatiblePackPairs
    {
        get => [..incompatiblePacks
            .Select(packCode => (packCode, packDescription: (PublicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDescription) ?? false) ? packDescription : null))
            .Where(t => t.packDescription is not null)
            .Select(t => new KeyValuePair<string, PackDescription>(t.packCode, t.packDescription!))];
        set =>
            incompatiblePacks = [..value.Select(kv => kv.Key)];
    }

    Version ParsedVersion
    {
        get =>
            System.Version.TryParse(version, out var v)
                ? v
                : new(0, 0, 0);
        set =>
            version = $"{value.Major}.{value.Minor}{(value.Build is -1 ? string.Empty : $".{value.Build}")}";
    }

    IEnumerable<KeyValuePair<string, PackDescription>> RequiredPackPairs
    {
        get => [..requiredPacks
            .Select(packCode => (packCode, packDescription: (PublicCatalogs.PackCatalog?.TryGetValue(packCode, out var packDescription) ?? false) ? packDescription : null))
            .Where(t => t.packDescription is not null)
            .Select(t => new KeyValuePair<string, PackDescription>(t.packCode, t.packDescription!))];
        set =>
            requiredPacks = [..value.Select(kv => kv.Key)];
    }

    public bool UrlEncouragingGuidanceOpen
    {
        get => urlEncouragingGuidanceOpen;
        set
        {
            urlEncouragingGuidanceOpen = value;
            if (value)
            {
                UrlProhibitiveGuidanceOpen = false;
            }
        }
    }

    public bool UrlProhibitiveGuidanceOpen
    {
        get => urlProhibitiveGuidanceOpen;
        set
        {
            urlProhibitiveGuidanceOpen = value;
            if (value)
            {
                UrlEncouragingGuidanceOpen = false;
            }
        }
    }

    public bool UsePublicPackCatalog
    {
        get => Settings.UsePublicPackCatalog;
        set => Settings.UsePublicPackCatalog = value;
    }

    public bool WriteScaffoldingToSubdirectory
    {
        get => Settings.WriteScaffoldingToSubdirectory;
        set => Settings.WriteScaffoldingToSubdirectory = value;
    }

    async Task<AddFileResult> AddFileAsync(FileInfo modFile)
    {
        modFile.Refresh();
        if (!modFile.Exists)
        {
            batchCancellationTokenSource?.Cancel();
            return AddFileResult.NonExistent;
        }
        if (components.Any(component => component.File is { } file && Path.GetFullPath(file.FullName) == Path.GetFullPath(modFile.FullName)))
            return AddFileResult.AlreadyAdded;
        ImmutableArray<ModFileManifestModel> manifests = [];
        IDisposable fileObjectModel;
        string? manifestResourceName = null;
        if (modFile.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
        {
            DataBasePackedFile dbpf;
            try
            {
                dbpf = await DataBasePackedFile.FromPathAsync(modFile.FullName, forReadOnly: false).ConfigureAwait(false);
            }
            catch
            {
                batchCancellationTokenSource?.Cancel();
                await DialogService.ShowErrorDialogAsync
                (
                    AppText.ManifestEditor_Error_InaccessiblePackage_Caption,
                    string.Format(AppText.ManifestEditor_Error_InaccessiblePackage_Text, modFile.FullName)
                ).ConfigureAwait(false);
                return AddFileResult.Unreadable;
            }
            manifests = [..(await ModFileManifestModel.GetModFileManifestsAsync(dbpf).ConfigureAwait(false))
                .Values
                .OrderBy(manifest => manifest.TuningName?.Length)
                .ThenBy(manifest => manifest.TuningName)];
            manifestResourceName = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.TuningName))?.TuningName ?? $"llamalogic:manifest_{Guid.NewGuid():n}";
            if (manifests.Length is > 1)
            {
                batchCancellationTokenSource?.Cancel();
                await DialogService.ShowInfoDialogAsync
                (
                    AppText.ManifestEditor_Info_SelectedMergedPackage_Caption,
                    string.Format(AppText.ManifestEditor_Info_SelectedMergedPackage_Text, modFile.FullName, manifestResourceName)
                ).ConfigureAwait(false);
            }
            fileObjectModel = dbpf;
        }
        else if (modFile.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
        {
            ZipArchive zipArchive;
            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                zipArchive = ZipFile.Open(modFile.FullName, ZipArchiveMode.Update);
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
            catch
            {
                batchCancellationTokenSource?.Cancel();
                await DialogService.ShowErrorDialogAsync
                (
                    AppText.ManifestEditor_Error_InaccessibleScriptArchive_Caption,
                    string.Format(AppText.ManifestEditor_Error_InaccessibleScriptArchive_Text, modFile.FullName)
                ).ConfigureAwait(false);
                return AddFileResult.Unreadable;
            }
            if (await ModFileManifestModel.GetModFileManifestAsync(zipArchive).ConfigureAwait(false) is { } manifest)
                manifests = [manifest];
            fileObjectModel = zipArchive;
        }
        else
        {
            batchCancellationTokenSource?.Cancel();
            await DialogService.ShowErrorDialogAsync
            (
                AppText.ManifestEditor_Error_UnrecognizedFileType_Caption,
                StringLocalizer["ManifestEditor_Error_UnrecognizedFileType_Text", modFile.FullName]
            ).ConfigureAwait(false);
            return AddFileResult.Unrecognized;
        }
        var componentName = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.Name))?.Name;
        var component = new ModComponent
        (
            manifests.Any(),
            modFile,
            fileObjectModel,
            manifestResourceName,
            true,
            null,
            null,
            null,
            null,
            null,
            string.Join(Environment.NewLine, manifests.SelectMany(manifest => manifest.Exclusivities).Distinct()),
            manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.MessageToTranslators))?.MessageToTranslators,
            manifests.FirstOrDefault(manifest => manifest.TranslationSubmissionUrl is not null)?.TranslationSubmissionUrl?.ToString(),
            string.Join(Environment.NewLine, manifests.SelectMany(manifest => manifest.Translators.Where(translator => !string.IsNullOrWhiteSpace(translator.Name)).Select(translator => $"{translator.Name}{Environment.NewLine}{translator.Language}")).Distinct()),
            componentName == name ? null : componentName,
            string.Join(Environment.NewLine, manifests.SelectMany(manifest => manifest.SubsumedHashes.Concat([manifest.Hash])).Select(hash => hash.ToHexString()).Distinct(StringComparer.OrdinalIgnoreCase))
        );
        component.PropertyChanged += HandleComponentPropertyChanged;
        components.Add(component);
        return manifests.Length is > 0
            ? AddFileResult.SucceededManifested
            : AddFileResult.Succeeded;
    }

    async Task<bool> AddFilesAsync(IReadOnlyList<FileInfo> modFiles)
    {
        var filesAdded = false;
        foreach (var modFile in modFiles)
        {
            var addFileResult = await AddFileAsync(modFile).ConfigureAwait(false);
            if (addFileResult is AddFileResult.Succeeded or AddFileResult.SucceededManifested)
            {
                filesAdded = true;
                RemoveComponentFromRequiredMods(components[^1], addFileResult);
            }
        }
        return filesAdded;
    }

    async Task AddRequiredManifestAsync(ModFileManifestModel manifest)
    {
        var requiredFeatures = string.Empty;
        if (manifest.Features.Count is > 0)
        {
            if (await DialogService.ShowSelectFeaturesDialogAsync(manifest) is not { } selectedFeatures)
                return;
            requiredFeatures = string.Join(Environment.NewLine, selectedFeatures);
        }
        var requiredMod = new ModRequirement
        (
            manifest.Name,
            manifest.Hash.ToHexString(),
            requiredFeatures,
            null,
            null,
            null,
            null,
            null,
            string.Join(Environment.NewLine, manifest.Creators),
            manifest.Url?.ToString(),
            manifest.Version
        );
        requiredMod.PropertyChanged += HandleRequiredModPropertyChanged;
        requiredMods.Add(requiredMod);
        StateHasChanged();
    }

    async Task AddSelectedFilesAsync(IReadOnlyList<FileInfo> modFiles)
    {
        loadingText = AppText.ManifestEditor_Loading_ReadingModFiles;
        isLoading = true;
        StateHasChanged();
        if (await Task.Run(async () => await AddFilesAsync(modFiles).ConfigureAwait(false)))
        {
            ComponentsStepSelectedComponent = null;
            UpdateComponentsStructure();
        }
        isLoading = false;
        StateHasChanged();
    }

    async Task<bool> CancelAtUserRequestAsync()
    {
        if (await OfferToCancelAsync())
        {
            await ResetAsync();
            return true;
        }
        return false;
    }

    async Task ComposeAsync()
    {
        await StaticDispatcher.DispatchAsync(() =>
        {
            isComposing = true;
            StateHasChanged();
        });

        Task updateStatusAsync(int compositionStep, string compositionStatus) =>
            StaticDispatcher.DispatchAsync(() =>
            {
                this.compositionStep = compositionStep;
                this.compositionStatus = compositionStatus;
                StateHasChanged();
            });

        string getComponentRelativePath(ModComponent component)
        {
            if (commonComponentDirectory is null)
                throw new NullReferenceException("common component directory is null");
            return component.File.FullName[(commonComponentDirectory.FullName.Length + 1)..];
        }

        static void addTransformedCollectionElements<TManifestElement, TEditorElement>(ICollection<TManifestElement> collection, IEnumerable<TEditorElement> elements, Func<TEditorElement, TManifestElement> manifestElementSelector)
        {
            foreach (var element in elements)
                collection.Add(manifestElementSelector(element));
        }

        static void addCollectionElements<TElement>(ICollection<TElement> collection, IEnumerable<TElement> elements) =>
            addTransformedCollectionElements(collection, elements, element => element);

        var toolingResourceTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())?.GetCustomAttribute<ResourceToolingMetadataAttribute>() is not null)
            .ToImmutableHashSet();
        var imageAndStringTableTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())?.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType is ResourceFileType.DirectDrawSurface or ResourceFileType.PortableNetworkGraphic or ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage)
            .Concat([ResourceType.StringTable])
            .ToImmutableHashSet();
        var tuningAndSimDataTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())?.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType is ResourceFileType.TuningMarkup)
            .Concat([ResourceType.SimData, ResourceType.CombinedTuning])
            .ToImmutableHashSet();

        var filesWithAlteredManifests = new List<FileInfo>();
        var succeeded = false;

        try
        {
            // stage 1: wipe all components clean of manifests
            try
            {
                foreach (var component in components)
                {
                    await updateStatusAsync(0, StringLocalizer["ManifestEditor_Composing_Status_RemovingManifests", getComponentRelativePath(component)]).ConfigureAwait(false);
                    if (component.FileObjectModel is DataBasePackedFile dbpf)
                        await ModFileManifestModel.DeleteModFileManifestsAsync(dbpf).ConfigureAwait(false);
                    else if (component.FileObjectModel is ZipArchive zipArchive)
                        ModFileManifestModel.DeleteModFileManifests(zipArchive);
                    else
                        throw new NotSupportedException($"Unsupported component file object model type {component?.GetType().FullName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("unhandled exception during manifest composition stage 1", ex);
            }

            var componentManifests = new Dictionary<ModComponent, ModFileManifestModel?>();

            // stage 2: initialize component manifests and compute hashes
            try
            {
                foreach (var component in components)
                {
                    var componentRelativePath = getComponentRelativePath(component);
                    await updateStatusAsync(1, StringLocalizer["ManifestEditor_Composing_Status_ComputingHash", componentRelativePath]).ConfigureAwait(false);
                    ImmutableArray<byte> hash;
                    HashSet<ResourceKey>? hashResourceKeys = null;
                    if (component.FileObjectModel is DataBasePackedFile dbpf)
                    {
                        var keys = await dbpf.GetKeysAsync().ConfigureAwait(false);
                        static HashSet<ResourceKey> getHashResourceKeys(IReadOnlyList<ResourceKey> keys, int hashingLevel, ImmutableHashSet<ResourceType> toolingResourceTypes, ImmutableHashSet<ResourceType> imageAndStringTableTypes, ImmutableHashSet<ResourceType> tuningAndSimDataTypes)
                        {
                            var result = new HashSet<ResourceKey>();
                            foreach (var key in keys)
                            {
                                var type = key.Type;
                                if (toolingResourceTypes.Contains(type))
                                    continue;
                                if (hashingLevel is < 2 && imageAndStringTableTypes.Contains(type))
                                    continue;
                                if (hashingLevel is < 1 && tuningAndSimDataTypes.Contains(type))
                                    continue;
                                result.Add(key);
                            }
                            return result;
                        }
                        var thisHashingLevel = hashingLevel - 1;
                        while (++thisHashingLevel is <= 2)
                        {
                            var possibleHashResourceKeys = getHashResourceKeys(keys, thisHashingLevel, toolingResourceTypes, imageAndStringTableTypes, tuningAndSimDataTypes);
                            if (possibleHashResourceKeys.Count is 0)
                                continue;
                            hashResourceKeys = possibleHashResourceKeys;
                            break;
                        }
                        if (hashResourceKeys is null)
                            continue;
                        hash = await ModFileManifestModel.GetModFileHashAsync(dbpf, hashResourceKeys).ConfigureAwait(false);
                    }
                    else if (component.FileObjectModel is ZipArchive zipArchive)
                        hash = ModFileManifestModel.GetModFileHash(zipArchive);
                    else
                        throw new NotSupportedException($"Unsupported component file object model type {component?.GetType().FullName}");
                    await updateStatusAsync(1, StringLocalizer["ManifestEditor_Composing_Status_InitializingManifest", componentRelativePath]).ConfigureAwait(false);
                    var tuningName = component.FileObjectModel is DataBasePackedFile
                        ? (
                            string.IsNullOrWhiteSpace(component.ManifestResourceName)
                            ? $"llamalogic:manifest_{Guid.NewGuid():n}"
                            : component.ManifestResourceName
                        )
                        : null;
                    var model = new ModFileManifestModel
                    {
                        Description = description,
                        ElectronicArtsPromoCode = string.IsNullOrWhiteSpace(electronicArtsPromoCode) ? null : electronicArtsPromoCode,
                        Hash = hash,
                        MessageToTranslators = string.IsNullOrWhiteSpace(component.MessageToTranslators) ? null : component.MessageToTranslators,
                        Name = string.IsNullOrWhiteSpace(component.Name) ? name : component.Name,
                        TranslationSubmissionUrl = component.TranslationSubmissionUrl is { } translationSubmissionUrl && Uri.TryCreate(translationSubmissionUrl, UriKind.Absolute, out var translationSubmissionUri) ? translationSubmissionUri : null,
                        TuningFullInstance = tuningName is null ? 0 : Fnv64.SetHighBit(Fnv64.GetHash(tuningName)),
                        TuningName = tuningName,
                        Url = Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl) ? parsedUrl : null,
                        Version = versionEnabled && !string.IsNullOrWhiteSpace(version) ? version : null
                    };
                    addCollectionElements(model.Creators, creators);
                    addCollectionElements(model.Exclusivities, component.Exclusivities);
                    addCollectionElements(model.Features, features);
                    if (hashResourceKeys is not null)
                        model.HashResourceKeys.UnionWith(hashResourceKeys);
                    addCollectionElements(model.IncompatiblePacks, incompatiblePacks);
                    addTransformedCollectionElements(model.RequiredMods, requiredMods, requiredMod =>
                    {
                        var model = new ModFileManifestModelRequiredMod
                        {
                            IgnoreIfHashAvailable = (requiredMod.IgnoreIfHashAvailable?.TryToByteSequence(out var availableSequence) ?? false) ? [.. availableSequence] : [],
                            IgnoreIfHashUnavailable = (requiredMod.IgnoreIfHashUnavailable?.TryToByteSequence(out var unavailableSequence) ?? false) ? [.. unavailableSequence] : [],
                            IgnoreIfPackAvailable = string.IsNullOrWhiteSpace(requiredMod.IgnoreIfPackAvailable) ? null : requiredMod.IgnoreIfPackAvailable,
                            IgnoreIfPackUnavailable = string.IsNullOrWhiteSpace(requiredMod.IgnoreIfPackUnavailable) ? null : requiredMod.IgnoreIfPackUnavailable,
                            Name = requiredMod.Name ?? string.Empty,
                            RequirementIdentifier = string.IsNullOrWhiteSpace(requiredMod.RequirementIdentifier) ? null : requiredMod.RequirementIdentifier,
                            Url = Uri.TryCreate(requiredMod.Url, UriKind.Absolute, out var parsedUrl) ? parsedUrl : null,
                            Version = string.IsNullOrWhiteSpace(requiredMod.Version) ? null : requiredMod.Version
                        };
                        addCollectionElements(model.Creators, requiredMod.Creators);
                        foreach (var hash in requiredMod.Hashes)
                            if (hash.TryToByteSequence(out var sequence))
                                model.Hashes.Add([.. sequence]);
                        addCollectionElements(model.RequiredFeatures, requiredMod.RequiredFeatures);
                        return model;
                    });
                    addCollectionElements(model.RequiredPacks, requiredPacks);
                    var hexHash = hash.ToHexString();
                    if (!component.SubsumedHashes.Contains(hexHash, StringComparer.OrdinalIgnoreCase))
                        filesWithAlteredManifests.Add(component.File);
                    addCollectionElements(model.SubsumedHashes, component.SubsumedHashes.Except([hexHash], StringComparer.OrdinalIgnoreCase).Select(hash => hash.TryToByteSequence(out var sequence) ? [.. sequence] : ImmutableArray<byte>.Empty));
                    addTransformedCollectionElements(model.Translators, component.Translators, t => new ModFileManifestModelTranslator { Name = t.name, Culture = t.language });
                    componentManifests.Add(component, model);
                }
                if (components.Count is 1
                    && components[0] is { } singleComponent
                    && !singleComponent.IsManifestPreExisting
                    && componentManifests[singleComponent] is { } manifest
                    && Settings.AutomaticallySubsumeIdenticallyCreditedSingleFileModsWhenInitializingAManifest)
                {
                    var subsumedHashes = new HashSet<ImmutableArray<byte>>(manifest.SubsumedHashes, ImmutableArrayEqualityComparer<byte>.Default);
                    var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    await foreach (var dbManifest in pbDbContext.ModFileManifests
                        .Where(mfm => mfm.Name == manifest.Name)
                        .Include(mfm => mfm.Creators)
                        .Include(mfm => mfm.CalculatedModFileManifestHash)
                        .Include(mfm => mfm.SubsumedHashes)
                        .AsAsyncEnumerable()
                        .ConfigureAwait(false))
                    {
                        if (dbManifest.Creators.Any(c => !manifest.Creators.Contains(c.Name)))
                            continue;
                        subsumedHashes.Add([..dbManifest.CalculatedModFileManifestHash.Sha256]);
                        subsumedHashes.UnionWith(dbManifest.SubsumedHashes.Select(sh => sh.Sha256.ToImmutableArray()));
                    }
                    manifest.SubsumedHashes.Clear();
                    manifest.SubsumedHashes.UnionWith(subsumedHashes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("unhandled exception during manifest composition stage 2", ex);
            }

            var crossReferenceRequirements = new List<(ModComponent component, ModFileManifestModelRequiredMod requiredModModel)>();

            // stage 3: generate cross reference requirements
            try
            {
                foreach (var component in components.Where(component => component.IsRequired))
                {
                    await updateStatusAsync(2, StringLocalizer["ManifestEditor_Composing_Status_GeneratingCrossReferenceRequirements", getComponentRelativePath(component)]).ConfigureAwait(false);
                    if (componentManifests.TryGetValue(component, out var manifest) && manifest?.Hash is { } hash && !hash.IsDefaultOrEmpty)
                    {
                        var model = new ModFileManifestModelRequiredMod
                        {
                            IgnoreIfHashAvailable = (component.IgnoreIfHashAvailable?.TryToByteSequence(out var availableSequence) ?? false) ? [.. availableSequence] : [],
                            IgnoreIfHashUnavailable = (component.IgnoreIfHashUnavailable?.TryToByteSequence(out var unavailableSequence) ?? false) ? [.. unavailableSequence] : [],
                            IgnoreIfPackAvailable = string.IsNullOrWhiteSpace(component.IgnoreIfPackAvailable) ? null : component.IgnoreIfPackAvailable,
                            IgnoreIfPackUnavailable = string.IsNullOrWhiteSpace(component.IgnoreIfPackUnavailable) ? null : component.IgnoreIfPackUnavailable,
                            Name = string.IsNullOrWhiteSpace(component.Name) ? name : component.Name,
                            RequirementIdentifier = string.IsNullOrWhiteSpace(component.RequirementIdentifier) ? null : component.RequirementIdentifier,
                            Url = Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl) ? parsedUrl : null,
                            Version = versionEnabled && !string.IsNullOrWhiteSpace(version) ? version : null
                        };
                        addCollectionElements(model.Creators, creators);
                        model.Hashes.Add(hash);
                        crossReferenceRequirements.Add((component, model));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("unhandled exception during manifest composition stage 3", ex);
            }

            // stage 4: add cross reference requirements
            try
            {
                foreach (var component in components)
                {
                    await updateStatusAsync(3, StringLocalizer["ManifestEditor_Composing_Status_AddingCrossReferenceRequirements", getComponentRelativePath(component)]).ConfigureAwait(false);
                    if (componentManifests.TryGetValue(component, out var manifest) && manifest is not null)
                    {
                        var requirementIndex = -1;
                        foreach (var (otherComponent, requiredModModel) in crossReferenceRequirements.Where(t => t.component != component && (string.IsNullOrWhiteSpace(component.RequirementIdentifier) || component.RequirementIdentifier != t.component.RequirementIdentifier)))
                            manifest.RequiredMods.Insert(++requirementIndex, requiredModModel);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("unhandled exception during manifest composition stage 1", ex);
            }

            if (!batchOverlayVisible)
                compositionResetEvent.Reset();

            // stage 5: commit manifests
            try
            {
                foreach (var component in components)
                    if (componentManifests.TryGetValue(component, out var manifest) && manifest is not null)
                    {
                        await updateStatusAsync(4, StringLocalizer["ManifestEditor_Composing_Status_SavingManifest", getComponentRelativePath(component)]).ConfigureAwait(false);
                        var fileType = component.FileObjectModel is ZipArchive ? ModsDirectoryFileType.ScriptArchive : ModsDirectoryFileType.Package;
                        if (component.FileObjectModel is DataBasePackedFile dbpf)
                        {
                            await ModFileManifestModel.SetModFileManifestAsync(dbpf, manifest).ConfigureAwait(false);
                            await dbpf.SaveAsync().ConfigureAwait(false);
                        }
                        else if (component.FileObjectModel is ZipArchive zipArchive)
                            await ModFileManifestModel.SetModFileManifestAsync(zipArchive, manifest).ConfigureAwait(false);
                        else
                            throw new NotSupportedException($"Unsupported component file object model type {component?.GetType().FullName}");
                        component.FileObjectModel.Dispose();
                        if (Settings.AutomaticallyCatalogOnComposition)
                        {
                            using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                            var (modFileHash, _) = await ModsDirectoryCataloger.GetModFileHashAsync(pbDbContext, fileType, await ModFileManifestModel.GetFileSha256HashAsync(component.File.FullName).ConfigureAwait(false)).ConfigureAwait(false);
                            modFileHash.ModFileManifests.Clear();
                            modFileHash.ModFileManifests.Add(await ModsDirectoryCataloger.TransformModFileManifestModelAsync(pbDbContext, modFileHash, manifest.Hash, manifest, fileType is ModsDirectoryFileType.ScriptArchive ? (ResourceKey?)null : new ResourceKey(ResourceType.SnippetTuning, 0x80000000, manifest.TuningFullInstance)).ConfigureAwait(false));
                            await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                if (versionEnabled && !string.IsNullOrWhiteSpace(version) && !string.IsNullOrEmpty(originalVersion))
                    foreach (var component in components)
                        if (component.File is { } file
                            && file.Directory is { } directory
                            && file.Name.Contains(originalVersion, StringComparison.Ordinal))
                        {
                            var newFilePath = Path.GetFullPath(Path.Combine(directory.FullName, file.Name.Replace(originalVersion, version, StringComparison.Ordinal)));
                            var newFile = new FileInfo(newFilePath);
                            if (Path.GetFullPath(file.FullName) != newFilePath && !newFile.Exists)
                            {
                                ManifestedModFileScaffolding.DeleteFor(file);
                                File.Move(file.FullName, newFilePath);
                                newFile.Refresh();
                                var tcs = new TaskCompletionSource();
                                StaticDispatcher.Dispatch(() =>
                                {
                                    component.File = newFile;
                                    tcs.SetResult();
                                });
                                await tcs.Task.ConfigureAwait(false);
                            }
                        }
                foreach (var component in components)
                    if (componentManifests.TryGetValue(component, out var manifest) && manifest is not null)
                    {
                        await updateStatusAsync(4, StringLocalizer["ManifestEditor_Composing_Status_CreatingScaffolding", getComponentRelativePath(component)]).ConfigureAwait(false);
                        var scaffolding = new ManifestedModFileScaffolding
                        {
                            ModName = (name ?? string.Empty).Trim(),
                            IsRequired = component.IsRequired,
                            ComponentName = (component.Name ?? string.Empty).Trim(),
                            HashingLevel = hashingLevel
                        };
                        foreach (var otherComponent in components.Except([component]))
                        {
                            var referencedModFile = new ManifestedModFileScaffoldingReferencedModFile
                            {
                                LocalAbsolutePath = otherComponent.File.FullName
                            };
                            if (component.File.Directory is { } componentFileDirectory)
                            {
                                var relativePath = Path.GetRelativePath(componentFileDirectory.FullName, referencedModFile.LocalAbsolutePath);
                                if (relativePath != referencedModFile.LocalAbsolutePath)
                                    referencedModFile.LocalRelativePath = relativePath;
                            }
                            scaffolding.OtherModComponents.Add(referencedModFile);
                        }
                        await scaffolding.CommitForAsync(component.File, Settings).ConfigureAwait(false);
                    }
            }
            catch (Exception ex)
            {
                throw new Exception("unhandled exception during manifest composition stage 5", ex);
            }

            succeeded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "unhandled exception during manifest composition");
            await DialogService.ShowErrorDialogAsync(AppText.ManifestEditor_Error_ManifestCompositionFailure_Caption, AppText.ManifestEditor_Error_ManifestCompositionFailure_Text);
        }

        if (!batchOverlayVisible)
            compositionResetEvent.Set();

        await updateStatusAsync(5, AppText.ManifestEditor_Composing_Status_Finished).ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

        if (!batchOverlayVisible && succeeded)
            await DialogService.ShowSuccessDialogAsync(AppText.ManifestEditor_Composing_Success_Caption, AppText.ManifestEditor_Composing_Success_Text).ConfigureAwait(false);

        await StaticDispatcher.DispatchAsync(async () =>
        {
            isComposing = false;
            await ResetAsync();
            await Task.Delay(25);
            StateHasChanged();
        }).ConfigureAwait(false);

        await Task.Delay(500).ConfigureAwait(false);

        batchTaskCompletionSource?.SetResult(filesWithAlteredManifests.ToImmutableArray());
    }

    public void Dispose()
    {
        foreach (var component in components)
        {
            component.PropertyChanged -= HandleComponentPropertyChanged;
            try
            {
                component.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // 🤷‍♂️
            }
        }
        Settings.PropertyChanged -= HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
        UserInterfaceMessaging.BeginManifestingModRequested -= HandleUserInterfaceMessagingBeginManifestingModRequested;
        RequestToRemainAlive = false;
    }

    async Task HandleAddCatalogedRequiredModOnClickedAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await DialogService.ShowSelectCatalogedModFileDialogAsync() is { } modFilePath
            && await ModFileSelector.GetSelectedModFileManifestAsync(pbDbContext, DialogService, new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", modFilePath))) is { } manifest)
            await AddRequiredManifestAsync(manifest);
    }

    async Task HandleAddFilesClickedAsync()
    {
        var modFiles = await ModFileSelector.SelectModFilesAsync();
        if (modFiles is null || modFiles.Count is 0)
            return;
        await AddSelectedFilesAsync(modFiles);
    }

    async Task HandleAddRequiredModOnClickedAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await ModFileSelector.SelectAModFileManifestAsync(pbDbContext, DialogService) is { } manifest)
            await AddRequiredManifestAsync(manifest);
    }

    void HandleCancelBatchProcess() =>
        batchCancellationTokenSource?.Cancel();

    Task<bool> HandleCancelOnClickAsync() =>
        CancelAtUserRequestAsync();

    void HandleComponentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ModComponent.File))
            StateHasChanged();
    }

    async Task HandleDropFilesClickedAsync()
    {
        static IEnumerable<FileInfo> scanPath(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var entry in Directory.GetFileSystemEntries(path))
                    foreach (var modFile in scanPath(entry))
                        yield return modFile;
                yield break;
            }
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists
                && (fileInfo.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase) || fileInfo.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase)))
                yield return fileInfo;
        }
        var modFiles = new List<FileInfo>();
        foreach (var path in await UserInterfaceMessaging.GetFilesFromDragAndDropAsync())
            modFiles.AddRange(scanPath(path));
        if (modFiles.Count is 0)
            return;
        await AddSelectedFilesAsync(modFiles);
    }

    async Task HandleDropRequiredModOnClickedAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await ModFileSelector.GetADroppedModFileManifestAsync(UserInterfaceMessaging, pbDbContext, DialogService) is { } manifest)
            await AddRequiredManifestAsync(manifest);
    }

    async Task HandleDuplicateComponentSettingsClickedAsync()
    {
        if (modComponentEditor is not null)
            await modComponentEditor.CommitPendingEntriesIfEmptyAsync();
        if (componentsStepSelectionMode is MudBlazor.SelectionMode.SingleSelection)
        {
            componentsStepSelectionMode = MudBlazor.SelectionMode.MultiSelection;
            StateHasChanged();
        }
        else
        {
            try
            {
                if (componentsStepSelectedComponent is not { } displayedComponent || componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync(AppText.ManifestEditor_Caution_DuplicateComponentSettings_Caption, AppText.ManifestEditor_Caution_DuplicateComponentSettings_Text))
                    return;
                var componentsToApplySettings = componentsStepSelectedComponents.ToImmutableHashSet();
                foreach (var componentToApplySettings in componentsToApplySettings)
                {
                    componentToApplySettings.IsRequired = displayedComponent.IsRequired;
                    componentToApplySettings.RequirementIdentifier = displayedComponent.RequirementIdentifier;
                    componentToApplySettings.IgnoreIfHashAvailable = displayedComponent.IgnoreIfHashAvailable;
                    componentToApplySettings.IgnoreIfHashUnavailable = displayedComponent.IgnoreIfHashUnavailable;
                    componentToApplySettings.IgnoreIfPackAvailable = displayedComponent.IgnoreIfPackAvailable;
                    componentToApplySettings.IgnoreIfPackUnavailable = displayedComponent.IgnoreIfPackUnavailable;
                    componentToApplySettings.Exclusivities = [..displayedComponent.Exclusivities];
                    componentToApplySettings.MessageToTranslators = displayedComponent.MessageToTranslators;
                    componentToApplySettings.TranslationSubmissionUrl = displayedComponent.TranslationSubmissionUrl;
                }
            }
            finally
            {
                await JSRuntime.InvokeVoidAsync("uncheckAllCheckboxes", ".manifest-editor-components-step-tree-view");
                componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
                StateHasChanged();
            }
        }
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.UsePublicPackCatalog))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    async Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        var activeIndex = stepper?.GetActiveIndex();
        if (targetIndex is 0)
        {
            if (activeIndex is not 0)
                await CancelAtUserRequestAsync();
            return true;
        }
        if (activeIndex is 2)
        {
            UrlProhibitiveGuidanceOpen = false;
            UrlEncouragingGuidanceOpen = false;
        }
        if (activeIndex is 3)
        {
            if (addRequiredModGuidanceOpen)
                addRequiredModGuidanceOpen = false;
        }
        if (direction is StepChangeDirection.Backward)
            return false;
        if (activeIndex is 0 && components.Count == 0)
        {
            if (selectStepFile is null)
            {
                await DialogService.ShowErrorDialogAsync(AppText.ManifestEditor_Error_SelectAModFileFirst_Caption, AppText.ManifestEditor_Error_SelectAModFileFirst_Text);
                return true;
            }
            loadingText = AppText.ManifestEditor_Loading_ExaminingModFile;
            isLoading = true;
            StateHasChanged();
            var selectStepFileAddResult = await AddFileAsync(selectStepFile);
            if (selectStepFileAddResult is AddFileResult.Succeeded or AddFileResult.SucceededManifested)
            {
                if (selectStepFileAddResult is AddFileResult.SucceededManifested)
                {
                    ImmutableArray<ModFileManifestModel> manifests;
                    var fileObjectModel = components[0].FileObjectModel;
                    if (fileObjectModel is DataBasePackedFile dbpf)
                        manifests = [..(await ModFileManifestModel.GetModFileManifestsAsync(dbpf))
                            .Values
                            .OrderBy(manifest => manifest.Name.Length)
                            .ThenBy(manifest => manifest.Name)];
                    else if (fileObjectModel is ZipArchive zipArchive)
                        manifests = [(await ModFileManifestModel.GetModFileManifestAsync(zipArchive))!];
                    else
                        throw new NotSupportedException($"Unsupported file object model {fileObjectModel?.GetType().Name}");
                    name = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.Name))?.Name ?? string.Empty;
                    description = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.Description))?.Description ?? string.Empty;
                    creators = manifests.SelectMany(manifest => manifest.Creators).Distinct().ToList().AsReadOnly();
                    creatorsChipSetField?.Refresh();
                    url = manifests.FirstOrDefault(manifest => manifest.Url is not null)?.Url?.ToString() ?? string.Empty;
                    requiredPacks = manifests.SelectMany(manifest => manifest.RequiredPacks).Select(packCode => packCode.ToUpperInvariant()).Distinct().ToList().AsReadOnly();
                    requiredPacksChipSetField?.Refresh();
                    electronicArtsPromoCode = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.ElectronicArtsPromoCode))?.ElectronicArtsPromoCode ?? string.Empty;
                    incompatiblePacks = manifests.SelectMany(manifest => manifest.IncompatiblePacks).Select(packCode => packCode.ToUpperInvariant()).Distinct().ToList().AsReadOnly();
                    incompatibleChipSetField?.Refresh();
                    requiredMods.AddRange(manifests
                        .SelectMany(manifest => manifest.RequiredMods)
                        .GroupBy(requiredMod => $"{string.Join(string.Empty, requiredMod.Hashes.Select(hash => hash.ToHexString()).Order())}|{requiredMod.RequirementIdentifier}|{requiredMod.IgnoreIfPackAvailable}|{requiredMod.IgnoreIfPackUnavailable}|{requiredMod.IgnoreIfHashAvailable}|{requiredMod.IgnoreIfHashUnavailable}")
                        .Select(functionallyIdenticalRequiredMods =>
                        {
                            ImmutableArray<ModFileManifestModelRequiredMod> prioritizedRequiredMods = [..functionallyIdenticalRequiredMods
                                .OrderBy(requiredMod => requiredMod.Name.Length)
                                .ThenBy(requiredMod => requiredMod.Name)];
                            var requiredMod = functionallyIdenticalRequiredMods.First();
                            return new ModRequirement
                            (
                                prioritizedRequiredMods.FirstOrDefault(requiredMod => !string.IsNullOrWhiteSpace(requiredMod.Name))?.Name,
                                string.Join(Environment.NewLine, requiredMod.Hashes.Select(hash => hash.ToHexString()).Distinct(StringComparer.OrdinalIgnoreCase)),
                                string.Join(Environment.NewLine, prioritizedRequiredMods.SelectMany(requiredMod => requiredMod.RequiredFeatures).Distinct()),
                                requiredMod.RequirementIdentifier,
                                requiredMod.IgnoreIfPackAvailable,
                                requiredMod.IgnoreIfPackUnavailable,
                                requiredMod.IgnoreIfHashAvailable.IsDefaultOrEmpty ? null : requiredMod.IgnoreIfHashAvailable.ToHexString(),
                                requiredMod.IgnoreIfHashUnavailable.IsDefaultOrEmpty ? null : requiredMod.IgnoreIfHashUnavailable.ToHexString(),
                                string.Join(Environment.NewLine, prioritizedRequiredMods.SelectMany(requiredMod => requiredMod.Creators).Distinct()),
                                prioritizedRequiredMods.FirstOrDefault(requiredMod => requiredMod.Url is not null)?.Url?.ToString(),
                                prioritizedRequiredMods.FirstOrDefault(requiredMod => !string.IsNullOrWhiteSpace(requiredMod.Version))?.Version
                            );
                        }));
                    version = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.Version))?.Version ?? string.Empty;
                    versionEnabled = !string.IsNullOrWhiteSpace(version);
                    features = manifests.SelectMany(manifest => manifest.Features).Distinct().ToList().AsReadOnly();
                }
                else
                {
                    batchContinue = true;
                    creators = [..Settings.DefaultCreatorsList.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                    var semVerPatternMatch = GetSemVerInModFileNamePattern().Match(selectStepFile.Name);
                    if (semVerPatternMatch.Success)
                    {
                        versionEnabled = true;
                        version = semVerPatternMatch.Value;
                    }
                }
                if (await ManifestedModFileScaffolding.TryLoadForAsync(selectStepFile, Settings) is { } scaffolding)
                {
                    components[0].IsRequired = scaffolding.IsRequired;
                    components[0].Name = string.IsNullOrWhiteSpace(scaffolding.ComponentName)
                        ? null
                        : scaffolding.ComponentName;
                    hashingLevel = scaffolding.HashingLevel;
                    foreach (var otherModComponent in scaffolding.OtherModComponents)
                    {
                        var addScaffoldedComponentResult = AddFileResult.NonExistent;
                        FileInfo? otherFileInfo = null;
                        if (otherModComponent.LocalRelativePath is { } localRelativePath
                            && !string.IsNullOrWhiteSpace(localRelativePath))
                            addScaffoldedComponentResult = await AddFileAsync(otherFileInfo = new FileInfo(Path.Combine(selectStepFile.DirectoryName!, localRelativePath)));
                        if (addScaffoldedComponentResult is AddFileResult.NonExistent
                            && !string.IsNullOrWhiteSpace(otherModComponent.LocalAbsolutePath))
                            addScaffoldedComponentResult = await AddFileAsync(otherFileInfo = new FileInfo(otherModComponent.LocalAbsolutePath));
                        if (addScaffoldedComponentResult is AddFileResult.NonExistent)
                            await DialogService.ShowErrorDialogAsync
                            (
                                AppText.ManifestEditor_Error_ScaffoldingReferenceBroken_Caption,
                                StringLocalizer["ManifestEditor_Error_ScaffoldingReferenceBroken_Text", otherModComponent.LocalAbsolutePath]
                            );
                        var otherComponent = components[^1];
                        RemoveComponentFromRequiredMods(otherComponent, addScaffoldedComponentResult);
                        if (otherFileInfo is not null && await ManifestedModFileScaffolding.TryLoadForAsync(otherFileInfo, Settings) is { } otherScaffolding)
                            otherComponent.Name = string.IsNullOrWhiteSpace(otherScaffolding.ComponentName)
                                ? null
                                : otherScaffolding.ComponentName;
                    }
                    if (!string.IsNullOrWhiteSpace(scaffolding.ModName))
                        name = scaffolding.ModName.Trim();
                }
                else if (selectStepFileAddResult is AddFileResult.SucceededManifested)
                {
                    if (batchOverlayVisible)
                        batchContinue = true;
                    else
                        await DialogService.ShowInfoDialogAsync
                        (
                            AppText.ManifestEditor_Info_MissingScaffolding_Caption,
                            StringLocalizer["ManifestEditor_Info_MissingScaffolding_Text"]
                        );
                }
                UpdateComponentsStructure();
            }
            isLoading = false;
            StateHasChanged();
            if (components.Count is 0)
                return true;
            originalVersion = version;
            RequestToRemainAlive = true;
        }
        if (activeIndex is 1)
        {
            if (creatorsChipSetField is not null)
                await creatorsChipSetField.CommitPendingEntryIfEmptyAsync();
            if (urlProhibitiveGuidanceOpen)
                urlProhibitiveGuidanceOpen = false;
            if (urlEncouragingGuidanceOpen)
                urlEncouragingGuidanceOpen = false;
        }
        if (activeIndex is 2)
        {
            if (modComponentEditor is not null)
                await modComponentEditor.CommitPendingEntriesIfEmptyAsync();
        }
        if (activeIndex is 3)
        {
            if (requiredPacksChipSetField is not null)
                await requiredPacksChipSetField.CommitPendingEntryIfEmptyAsync();
            if (incompatibleChipSetField is not null)
                await incompatibleChipSetField.CommitPendingEntryIfEmptyAsync();
            foreach (var requiredMod in requiredMods)
                await requiredMod.CommitPendingEntriesIfEmptyAsync();
        }
        if (activeIndex is 4)
        {
            if (featuresChipSetField is not null)
                await featuresChipSetField.CommitPendingEntryIfEmptyAsync();
        }
        if (targetIndex == 6)
        {
            RefreshCompositionStepMessages();
        }
        if (targetIndex == 7)
        {
            if (confirmationStepMessages.Any(csm => csm.severity is Severity.Error))
            {
                batchCancellationTokenSource?.Cancel();
                return true;
            }
            if (confirmationStepMessages.Any(csm => csm.severity is Severity.Warning) && !await DialogService.ShowCautionDialogAsync(AppText.ManifestEditor_Caution_ConfirmWithWarnings_Caption, AppText.ManifestEditor_Caution_ConfirmWithWarnings_Text))
            {
                batchCancellationTokenSource?.Cancel();
                return true;
            }
            for (var i = 0; i < 6; ++i)
                await stepper!.CompleteStep(i, false);
            _ = Task.Run(ComposeAsync);
        }
        return false;
    }

    void HandleQuickSemanticMajorOnClick()
    {
        var v = ParsedVersion;
        ParsedVersion = new(v.Major + 1, 0);
    }

    void HandleQuickSemanticMinorOnClick()
    {
        var v = ParsedVersion;
        ParsedVersion = new(v.Major, v.Minor + 1);
    }

    void HandleQuickSemanticPatchOnClick()
    {
        var v = ParsedVersion;
        ParsedVersion = new(v.Major, v.Minor, Math.Max(v.Build, 0) + 1);
    }

    void HandlePublicCatalogsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPublicCatalogs.PackCatalog))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    async Task HandleOpenDownloadPageUrlInBrowserAsync()
    {
        try
        {
            await Browser.OpenAsync(url, BrowserLaunchMode.External);
        }
        catch
        {
        }
    }

    async Task HandleRemoveFilesClickedAsync()
    {
        if (componentsStepSelectionMode is MudBlazor.SelectionMode.SingleSelection)
        {
            componentsStepSelectionMode = MudBlazor.SelectionMode.MultiSelection;
            StateHasChanged();
        }
        else
        {
            try
            {
                if (componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync(AppText.ManifestEditor_Caution_RemoveModFiles_Caption, AppText.ManifestEditor_Caution_RemoveModFiles_Text))
                    return;
                var componentsToRemove = componentsStepSelectedComponents.ToImmutableHashSet();
                components.RemoveAll(componentsToRemove.Contains);
                foreach (var componentToRemove in componentsToRemove)
                {
                    componentToRemove.PropertyChanged -= HandleComponentPropertyChanged;
                    componentToRemove.Dispose();
                }
                UpdateComponentsStructure();
                if (componentsStepSelectedComponent is not null && componentsToRemove.Contains(componentsStepSelectedComponent))
                    componentsStepSelectedComponent = null;
            }
            finally
            {
                await JSRuntime.InvokeVoidAsync("uncheckAllCheckboxes", ".manifest-editor-components-step-tree-view");
                componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
                StateHasChanged();
            }
        }
    }

    async Task HandleRemoveRequiredModOnClickedAsync(ModRequirement requiredMod)
    {
        if (!await DialogService.ShowCautionDialogAsync
            (
                AppText.ManifestEditor_Caution_RemoveRequiredMod_Caption,
                StringLocalizer["ManifestEditor_Caution_RemoveRequiredMod_Text", requiredMod.Name ?? AppText.ManifestEditor_Caution_RemoveRequiredMod_Text_ModNameFallback]
            ))
            return;
        requiredMods.Remove(requiredMod);
        StateHasChanged();
    }

    void HandleRequiredModPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ModRequirement.Name))
            StateHasChanged();
    }

    async Task HandleUpdateAllManifestsInAFolderAsync()
    {
        if (stepper is null
            || await FolderPicker.PickAsync() is not { } folderPickerResult
            || !folderPickerResult.IsSuccessful)
            return;
        var (folderFromPicker, exception) = folderPickerResult;
        if (exception is not null)
            Logger.LogWarning(exception, "encountered unhandled exception while selecting a folder for batch manifest update");
        if (folderFromPicker is null || exception is not null)
            return;
        var (folderPath, _) = folderFromPicker;
        var processingDirectory = new DirectoryInfo(folderPath);
        if (!processingDirectory.Exists)
            return;
        var modFiles = processingDirectory
            .GetFiles("*.*", SearchOption.AllDirectories)
            .Where(mf => mf.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase) || mf.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var unscaffolded = new List<FileInfo>();
        var changedManifests = new List<FileInfo>();
        var unchangedManifests = new List<FileInfo>();
        batchOverlayMax = modFiles.Count;
        batchOverlayValue = 0;
        batchOverlayFilename = string.Empty;
        batchOverlayVisible = true;
        batchCancellationTokenSource = new();
        StateHasChanged();
        var token = batchCancellationTokenSource.Token;
        compositionResetEvent.Reset();
        try
        {
            while (modFiles.Count is > 0)
            {
                token.ThrowIfCancellationRequested();
                var modFile = modFiles[0];
                ++batchOverlayValue;
                batchOverlayFilename = modFile.Name;
                selectStepFile = modFile;
                batchContinue = false;
                await stepper.CompleteStep(0);
                if (batchContinue)
                {
                    unscaffolded.Add(modFile);
                    await ResetAsync();
                    modFiles.RemoveAt(0);
                    continue;
                }
                for (var step = 1; step < 6; ++step)
                {
                    token.ThrowIfCancellationRequested();
                    await stepper.CompleteStep(step);
                }
                token.ThrowIfCancellationRequested();
                batchOverlayValue += modFiles.RemoveAll(mf => components.Any(c => Path.GetFullPath(c.File.FullName) == Path.GetFullPath(mf.FullName))) - 1;
                var potentiallyChangedManifests = components.Select(c => c.File).ToImmutableArray();
                batchTaskCompletionSource = new();
                await stepper.CompleteStep(6);
                token.ThrowIfCancellationRequested();
                var changedManifestsForThisMod = await batchTaskCompletionSource.Task;
                StateHasChanged();
                changedManifests.AddRange(changedManifestsForThisMod);
                unchangedManifests.AddRange(potentiallyChangedManifests.Where(mf => !changedManifestsForThisMod.Any(cmf => Path.GetFullPath(cmf.FullName) == Path.GetFullPath(mf.FullName))));
            }
        }
        catch (OperationCanceledException)
        {
            StateHasChanged();
        }
        finally
        {
            batchCancellationTokenSource.Dispose();
            batchCancellationTokenSource = null;
            batchTaskCompletionSource = null;
            batchOverlayVisible = false;
            StateHasChanged();
            compositionResetEvent.Set();
        }
        if (changedManifests.Count is > 0 || unchangedManifests.Count is > 0)
        {
            var commonPathLength = Path.GetFullPath(processingDirectory.FullName).Length + 1;
            var reportElements = new List<string>();
            if (changedManifests.Count is > 0)
                reportElements.Add(StringLocalizer["ManifestEditor_BatchUpdate_Report_ManifestsUpdated", string.Join(Environment.NewLine, changedManifests.OrderBy(mf => mf.FullName).Select(cm => $"- {cm.FullName[commonPathLength..]}"))]);
            if (unchangedManifests.Count is > 0)
                reportElements.Add(StringLocalizer["ManifestEditor_BatchUpdate_Report_ManifestsNotUpdated", string.Join(Environment.NewLine, unchangedManifests.OrderBy(mf => mf.FullName).Select(um => $"- {um.FullName[commonPathLength..]}"))]);
            if (unscaffolded.Count is > 0)
                reportElements.Add(StringLocalizer["ManifestEditor_BatchUpdate_Report_ModFilesNotScaffolded", string.Join(Environment.NewLine, unscaffolded.OrderBy(mf => mf.FullName).Select(u => $"- {u.FullName[commonPathLength..]}"))]);
            var report = string.Join($"{Environment.NewLine}{Environment.NewLine}", reportElements);
            if (await DialogService.ShowQuestionDialogAsync(AppText.ManifestEditor_Question_CopyBatchUpdateReportToClipboard_Caption, report, false, true) ?? false)
            {
                await Clipboard.SetTextAsync(report);
                await DialogService.ShowSuccessDialogAsync(AppText.ManifestEditor_Success_BatchUpdateReportCopiedToClipboard_Caption, AppText.ManifestEditor_Success_BatchUpdateReportCopiedToClipboard_Text);
            }
        }
        else
            await DialogService.ShowInfoDialogAsync(AppText.ManifestEditor_Info_BatchUpdateNoModFilesManifested_Caption, AppText.ManifestEditor_Info_BatchUpdateNoModFilesManifested_Text);
    }

    async void HandleUserInterfaceMessagingBeginManifestingModRequested(object? sender, BeginManifestingModRequestedEventArgs e)
    {
        if (stepper is null || stepper.GetActiveIndex() is not 0)
            return;
        selectStepFile = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Mods", e.ModFilePath));
        await stepper.SetActiveIndex(6);
        StateHasChanged();
    }

    Task<bool> OfferToCancelAsync() =>
        DialogService.ShowCautionDialogAsync(AppText.ManifestEditor_Caution_OfferToCancel_Caption, AppText.ManifestEditor_Caution_OfferToCancel_Text);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
        UserInterfaceMessaging.BeginManifestingModRequested += HandleUserInterfaceMessagingBeginManifestingModRequested;
    }

    void RefreshCompositionStepMessages()
    {
        var messages = new List<(Severity severity, string? icon, string message)>();
        messages.AddRange(components
            .Where(component => component.ManifestResourceName is not null)
            .GroupBy(component => component.ManifestResourceName)
            .Where(componentsWithSameManifestName => componentsWithSameManifestName.Count() > 1)
            .Select(componentsWithSameManifestName =>
            (
                Severity.Error,
                (string?)MaterialDesignIcons.Normal.FormatLetterMatches,
                StringLocalizer["ManifestEditor_Confirm_Error_NonUniqueManifestSnippetTuningNames", componentsWithSameManifestName.Key!, string.Join(Environment.NewLine, componentsWithSameManifestName.Select(component => $"* `{component.File.FullName[(commonComponentDirectory!.FullName.Length + 1)..]}`"))].Value
            )));
        messages.AddRange(components
            .Where(component => component.FileObjectModel is DataBasePackedFile && string.IsNullOrWhiteSpace(component.ManifestResourceName))
            .Select(component =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.DiceMultiple,
                StringLocalizer["ManifestEditor_Confirm_Warning_BlankManifestSnippetTuningName", component.File.FullName[(commonComponentDirectory!.FullName.Length + 1)..]].Value
            )));
        if (string.IsNullOrWhiteSpace(name))
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                StringLocalizer["ManifestEditor_Confirm_Warning_BlankModName"].Value
            ));
        if (creators.Count is 0)
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.AccountSearch,
                StringLocalizer["ManifestEditor_Confirm_Warning_NoCreators"].Value
            ));
        if (string.IsNullOrWhiteSpace(url) || !GetUrlPattern().IsMatch(url))
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.CloudSearch,
                StringLocalizer["ManifestEditor_Confirm_Warning_BlankDownloadPageUrl"].Value
            ));
        messages.AddRange(requiredMods
            .Select((requiredMod, index) => (requiredMod, index))
            .Where(t => string.IsNullOrWhiteSpace(t.requiredMod.Name))
            .Select(t =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                StringLocalizer["ManifestEditor_Confirm_Warning_BlankRequiredModName", (t.index + 1).Ordinalize()].Value
            )));
        messages.AddRange(requiredMods
            .Where(requiredMod => !string.IsNullOrWhiteSpace(requiredMod.Name) && (string.IsNullOrWhiteSpace(requiredMod.Url) || !Uri.TryCreate(requiredMod.Url, UriKind.Absolute, out _)))
            .Select(requiredMod =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                StringLocalizer["ManifestEditor_Confirm_Warning_BlankRequiredModDownloadPageUrl", requiredMod.Name!].Value
            )));
        if (requiredPacks.Count is > 0 && string.IsNullOrWhiteSpace(electronicArtsPromoCode))
            messages.Add
            ((
                Severity.Normal,
                (string?)MaterialDesignIcons.Normal.AccountCash,
                StringLocalizer["ManifestEditor_Confirm_Note_BlankPromoCode"].Value
            ));

        confirmationStepMessages = [..messages];
    }

    void RemoveComponentFromRequiredMods(ModComponent component, AddFileResult addFileResult)
    {
        if (addFileResult is AddFileResult.SucceededManifested)
        {
            var wasRequired = false;
            var componentHashes = component.SubsumedHashes.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var requiredMod in requiredMods)
                if (requiredMod.Hashes.Any(componentHashes.Contains))
                {
                    wasRequired = true;
                    requiredMod.Hashes = [..requiredMod.Hashes.Where(hash => !componentHashes.Contains(hash))];
                    component.RequirementIdentifier = requiredMod.RequirementIdentifier;
                    component.IgnoreIfPackAvailable = requiredMod.IgnoreIfPackAvailable;
                    component.IgnoreIfPackUnavailable = requiredMod.IgnoreIfPackUnavailable;
                    component.IgnoreIfHashAvailable = requiredMod.IgnoreIfHashAvailable;
                    component.IgnoreIfHashUnavailable = requiredMod.IgnoreIfHashUnavailable;
                }
            component.IsRequired = wasRequired;
            requiredMods.RemoveAll(requiredMod => !requiredMod.Hashes.Any());
        }
    }

    async Task ResetAsync()
    {
        compositionStep = 0;
        compositionStatus = string.Empty;
        hashingLevel = 1;
        await (featuresChipSetField?.ClearAsync() ?? Task.CompletedTask);
        version = string.Empty;
        versionEnabled = false;
        requiredMods.Clear();
        await (incompatibleChipSetField?.ClearAsync() ?? Task.CompletedTask);
        electronicArtsPromoCode = string.Empty;
        await (requiredPacksChipSetField?.ClearAsync() ?? Task.CompletedTask);
        await (requirementsStepForm?.ResetAsync() ?? Task.CompletedTask);
        url = string.Empty;
        await (creatorsChipSetField?.ClearAsync() ?? Task.CompletedTask);
        description = string.Empty;
        name = string.Empty;
        await (detailsStepForm?.ResetAsync() ?? Task.CompletedTask);
        componentsStepSelectedComponent = null;
        componentsStepSelectionMode = MudBlazor.SelectionMode.SingleSelection;
        foreach (var component in components)
        {
            component.PropertyChanged -= HandleComponentPropertyChanged;
            try
            {
                component.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // 🤷‍♂️
            }
        }
        components.Clear();
        UpdateComponentsStructure();
        selectStepFileType = ModsDirectoryFileType.Ignored;
        selectStepFile = null;
        await (selectStepForm?.ResetAsync() ?? Task.CompletedTask);
        stepper?.Reset();
        stepper?.ForceRender();
        isLoading = false;
        StateHasChanged();
        RequestToRemainAlive = false;
    }

    void UpdateComponentsStructure()
    {
        var previousCommonComponentDirectoryFullName = commonComponentDirectory is not null
            ? Path.GetFullPath(commonComponentDirectory.FullName)
            : string.Empty;
        var componentPathsSplitBySegment = components
            .Select(component => component.File.FullName)
            .Select(path => path.Split(Path.DirectorySeparatorChar))
            .ToImmutableArray();
        if (componentPathsSplitBySegment.Length == 0)
            commonComponentDirectory = null;
        else
        {
            var commonPathSegments = componentPathsSplitBySegment[0];
            foreach (var splitPath in componentPathsSplitBySegment.Skip(1))
                commonPathSegments = commonPathSegments
                    .Zip(splitPath, (part1, part2) => part1 == part2 ? part1 : null)
                    .TakeWhile(part => part != null)
                    .ToArray()!;
            var commonPath = 
#if MACCATALYST
                "/" +
#endif
                Path.Combine(commonPathSegments);
            if (Directory.Exists(commonPath))
                commonComponentDirectory = new(commonPath);
            else if (File.Exists(commonPath))
                commonComponentDirectory = new FileInfo(commonPath).Directory;
            else
                throw new InvalidOperationException("common component directory algorithm failed");
        }
        var currentCommonComponentDirectoryFullName = commonComponentDirectory is not null
            ? Path.GetFullPath(commonComponentDirectory.FullName)
            : string.Empty;
        if (!currentCommonComponentDirectoryFullName.Equals(previousCommonComponentDirectoryFullName, StringComparison.Ordinal))
            componentTreeItemData.Clear();
        static IEnumerable<ModComponent> collectComponentsFromTreeItemData(IReadOnlyList<TreeItemData<ModComponent?>> readOnlyList)
        {
            foreach (var node in readOnlyList)
            {
                if (node.Value is { } component)
                    yield return component;
                else if (node.Children is { } children)
                    foreach (var childComponent in collectComponentsFromTreeItemData(children))
                        yield return childComponent;
            }
        }
        var comparer = PlatformFunctions.FileSystemStringComparison switch
        {
            StringComparison.Ordinal => StringComparer.Ordinal,
            _ => StringComparer.OrdinalIgnoreCase
        };
        TreeItemData<ModComponent?> integrateTreeItemData(IList<TreeItemData<ModComponent?>> list, string text, ModComponent? component)
        {
            var incorporationIndex = 0;
            foreach (var node in list)
            {
                if (comparer.Compare(node.Text, text) < 0)
                {
                    ++incorporationIndex;
                    continue;
                }
                break;
            }
            var treeItem = component is null
                ? new TreeItemData<ModComponent?>
                {
                    Children = [],
                    Expandable = true,
                    Expanded = true,
                    Icon = MaterialDesignIcons.Normal.Folder,
                    Text = text
                }
                : new TreeItemData<ModComponent?>
                {
                    Children = null,
                    Expandable = false,
                    Expanded = false,
                    Icon = component.File.Extension switch
                    {
                        string packageExtension when packageExtension.Equals(".package", StringComparison.OrdinalIgnoreCase) =>
                            MaterialDesignIcons.Normal.PackageVariantClosedCheck,
                        string ts4scriptExtension when ts4scriptExtension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase) =>
                            MaterialDesignIcons.Normal.SourceBranchCheck,
                        _ =>
                            MaterialDesignIcons.Normal.FileCancel
                    },
                    Text = text,
                    Value = component
                };
            list.Insert(incorporationIndex, treeItem);
            return treeItem;
        }
        var incorporatedComponents = collectComponentsFromTreeItemData(componentTreeItemData).ToImmutableArray();
        foreach (var componentToIncorporate in components.Except(incorporatedComponents, ModComponentFullPathEqualityComparer.Default))
        {
            var treeItemData = componentTreeItemData;
            var relativePath = componentToIncorporate.File.FullName[commonComponentDirectory!.FullName.Length..];
            var segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            for (var s = 0; s < segments.Length - 1; ++s)
            {
                var segment = segments[s];
                var treeItem = treeItemData.FirstOrDefault(tid => tid.Text == segment)
                    ?? integrateTreeItemData(treeItemData, segment, null);
                treeItemData = treeItem.Children!;
            }
            integrateTreeItemData(treeItemData, componentToIncorporate.File.Name, componentToIncorporate);
        }
        bool detachComponent(List<TreeItemData<ModComponent?>> list, ModComponent component)
        {
            if (list.RemoveAll(node => node.Value == component) > 0)
                return true;
            foreach (var node in list)
                if (node.Children is { } children && detachComponent(children, component))
                {
                    if (children.Count == 0)
                        list.Remove(node);
                    return true;
                }
            return false;
        }
        foreach (var componentToDetach in incorporatedComponents.Except(components, ModComponentFullPathEqualityComparer.Default))
            detachComponent(componentTreeItemData, componentToDetach);
    }
}

file class ImmutableArrayEqualityComparer<T> :
    IEqualityComparer<ImmutableArray<T>>
{
    public static ImmutableArrayEqualityComparer<T> Default { get; } = new();

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y) =>
        x.SequenceEqual(y);

    public int GetHashCode([DisallowNull] ImmutableArray<T> obj)
    {
        var hashCode = new HashCode();
        foreach (var element in obj)
            hashCode.Add(element);
        return hashCode.ToHashCode();
    }
}
