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
        "Lenient",
        "Moderate (Recommended)",
        "Strict"
    ];
    static readonly AsyncManualResetEvent compositionResetEvent = new(true);

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
                await DialogService.ShowErrorDialogAsync("Package corrupt, damaged, or locked",
                    $"""
                    I was unable to read this file as a valid Maxis DataBase Packed File with exclusive access:
                    `{modFile.FullName}`<br /><br />
                    <iframe src="https://giphy.com/embed/1g2JyW7p6mtZc6bOEY" width="480" height="269" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/guavajuice-guava-juice-roi-1g2JyW7p6mtZc6bOEY">via GIPHY</a></p>
                    """).ConfigureAwait(false);
                return AddFileResult.Unreadable;
            }
            manifests = [..(await ModFileManifestModel.GetModFileManifestsAsync(dbpf).ConfigureAwait(false))
                .Values
                .OrderBy(manifest => manifest.TuningName?.Length)
                .ThenBy(manifest => manifest.TuningName)];
            if (manifests.Length is > 1)
            {
                batchCancellationTokenSource?.Cancel();
                await DialogService.ShowInfoDialogAsync("Package is the result of multiple manifested packages which were merged",
                    $"""
                    Just so you're aware... this file has multiple manifests. Frankly, not the best. Don't worry, I'll tidy things up and leave it with just one when we finish here.
                    `{modFile.FullName}`<br /><br />
                    From the available **Manifest Snippet Tuning Resource Names**, I selected `{manifestResourceName}`. You can change that if you want by selecting this file on the **Components** step.<br /><br />
                    <iframe src="https://giphy.com/embed/8Fla28qk2RGlYa2nXr" width="480" height="259" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/8Fla28qk2RGlYa2nXr">via GIPHY</a></p>
                    """).ConfigureAwait(false);
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
                await DialogService.ShowErrorDialogAsync("Script archive corrupt, damaged, or locked",
                    $"""
                    I was unable to read this file as a valid ZIP archive with exclusive access:
                    `{modFile.FullName}`<br /><br />
                    <iframe src="https://giphy.com/embed/3o72EYhVhAYFJ4rv68" width="480" height="269" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/ghostbustersmovies-ghostbusters-original-3o72EYhVhAYFJ4rv68">via GIPHY</a></p>
                    """).ConfigureAwait(false);
                return AddFileResult.Unreadable;
            }
            if (await ModFileManifestModel.GetModFileManifestAsync(zipArchive).ConfigureAwait(false) is { } manifest)
                manifests = [manifest];
            fileObjectModel = zipArchive;
        }
        else
        {
            batchCancellationTokenSource?.Cancel();
            await DialogService.ShowErrorDialogAsync("new extension... who dis?",
                $"""
                What even is this?
                `{modFile.FullName}`<br /><br />
                <iframe src="https://giphy.com/embed/WRQBXSCnEFJIuxktnw" width="480" height="307" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/math-lady-meme-WRQBXSCnEFJIuxktnw">via GIPHY</a></p>
                """).ConfigureAwait(false);
            return AddFileResult.Unrecognized;
        }
        var componentName = manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.Name))?.Name;
        var component = new ModComponent
        (
            modFile,
            fileObjectModel,
            manifests.FirstOrDefault(manifest => !string.IsNullOrWhiteSpace(manifest.TuningName))?.TuningName ?? (fileObjectModel is DataBasePackedFile ? $"llamalogic:manifest_{Guid.NewGuid():n}" : null),
            true,
            null,
            null,
            null,
            null,
            null,
            string.Join(Environment.NewLine, manifests.SelectMany(manifest => manifest.Exclusivities).Distinct()),
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

        string getComponentRelativePath(ModComponent component) =>
            component.File.FullName[(commonComponentDirectory!.FullName.Length + 1)..];

        static void addTransformedCollectionElements<TManifestElement, TEditorElement>(ICollection<TManifestElement> collection, IEnumerable<TEditorElement> elements, Func<TEditorElement, TManifestElement> manifestElementSelector)
        {
            foreach (var element in elements)
                collection.Add(manifestElementSelector(element));
        }

        static void addCollectionElements<TElement>(ICollection<TElement> collection, IEnumerable<TElement> elements) =>
            addTransformedCollectionElements(collection, elements, element => element);

        var toolingResourceTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())!.GetCustomAttribute<ResourceToolingMetadataAttribute>() is not null)
            .ToImmutableHashSet();
        var imageAndStringTableTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())!.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType is ResourceFileType.DirectDrawSurface or ResourceFileType.PortableNetworkGraphic)
            .Concat([ResourceType.StringTable])
            .ToImmutableHashSet();
        var tuningAndSimDataTypes = Enum.GetValues<ResourceType>()
            .Where(type => typeof(ResourceType).GetField(type.ToString())!.GetCustomAttribute<ResourceFileTypeAttribute>()?.ResourceFileType is ResourceFileType.TuningMarkup)
            .Concat([ResourceType.SimData, ResourceType.CombinedTuning])
            .ToImmutableHashSet();

        var filesWithAlteredManifests = new List<FileInfo>();

        try
        {
            // stage 1: wipe all components clean of manifests
            foreach (var component in components)
            {
                await updateStatusAsync(0, $"Removing any manifests from `{getComponentRelativePath(component)}`").ConfigureAwait(false);
                if (component.FileObjectModel is DataBasePackedFile dbpf)
                    foreach (var manifestResourceKey in (await ModFileManifestModel.GetModFileManifestsAsync(dbpf).ConfigureAwait(false)).Keys)
                        dbpf.Delete(manifestResourceKey); // 😱
                else if (component.FileObjectModel is ZipArchive zipArchive)
                    while (zipArchive.Entries.FirstOrDefault(entry => entry.FullName.Equals("llamalogic.modfilemanifest.yml", StringComparison.OrdinalIgnoreCase)) is { } entry)
                        entry.Delete(); // 😱
                else
                    throw new NotSupportedException($"Unsupported component file object model type {component?.GetType().FullName}");
            }

            // stage 2: initialize component manifests and compute hashes
            var componentManifests = new Dictionary<ModComponent, ModFileManifestModel?>();
            foreach (var component in components)
            {
                var componentRelativePath = getComponentRelativePath(component);
                await updateStatusAsync(1, $"Computing manfiest hash for `{componentRelativePath}`").ConfigureAwait(false);
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
                await updateStatusAsync(1, $"Initializing manfiest for `{componentRelativePath}`").ConfigureAwait(false);
                var tuningName = component.FileObjectModel is DataBasePackedFile
                    ? (
                        string.IsNullOrWhiteSpace(component.ManifestResourceName)
                        ? $"llamalogic:manifest_{Guid.NewGuid():n}"
                        : component.ManifestResourceName
                    )
                    : null;
                var model = new ModFileManifestModel
                {
                    ElectronicArtsPromoCode = string.IsNullOrWhiteSpace(electronicArtsPromoCode) ? null : electronicArtsPromoCode,
                    Hash = hash,
                    Name = string.IsNullOrWhiteSpace(component.Name) ? name : component.Name,
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
                            model.Hashes.Add([..sequence]);
                    addCollectionElements(model.RequiredFeatures, requiredMod.RequiredFeatures);
                    return model;
                });
                addCollectionElements(model.RequiredPacks, requiredPacks);
                var hexHash = hash.ToHexString();
                if (!component.SubsumedHashes.Contains(hexHash, StringComparer.OrdinalIgnoreCase))
                    filesWithAlteredManifests.Add(component.File);
                addCollectionElements(model.SubsumedHashes, component.SubsumedHashes.Except([hexHash], StringComparer.OrdinalIgnoreCase).Select(hash => hash.TryToByteSequence(out var sequence) ? [.. sequence] : ImmutableArray<byte>.Empty));
                componentManifests.Add(component, model);
            }

            // stage 3: generate cross reference requirements
            var crossReferenceRequirements = new List<(ModComponent component, ModFileManifestModelRequiredMod requiredModModel)>();
            foreach (var component in components.Where(component => component.IsRequired))
            {
                var componentRelativePath = getComponentRelativePath(component);
                await updateStatusAsync(2, $"Generating cross-reference requirement for `{componentRelativePath}`").ConfigureAwait(false);
                if (componentManifests.TryGetValue(component, out var manifest) && manifest?.Hash is { } hash && !hash.IsDefaultOrEmpty)
                {
                    var model = new ModFileManifestModelRequiredMod
                    {
                        IgnoreIfHashAvailable = (component.IgnoreIfHashAvailable?.TryToByteSequence(out var availableSequence) ?? false) ? [..availableSequence] : [],
                        IgnoreIfHashUnavailable = (component.IgnoreIfHashUnavailable?.TryToByteSequence(out var unavailableSequence) ?? false) ? [..unavailableSequence] : [],
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

            // stage 4: add cross reference requirements
            foreach (var component in components)
            {
                var componentRelativePath = getComponentRelativePath(component);
                await updateStatusAsync(3, $"Adding cross-reference requirements to `{componentRelativePath}`").ConfigureAwait(false);
                if (componentManifests.TryGetValue(component, out var manifest) && manifest is not null)
                {
                    var requirementIndex = -1;
                    foreach (var (otherComponent, requiredModModel) in crossReferenceRequirements.Where(t => t.component != component && (string.IsNullOrWhiteSpace(component.RequirementIdentifier) || component.RequirementIdentifier != t.component.RequirementIdentifier)))
                        manifest.RequiredMods.Insert(++requirementIndex, requiredModModel);
                }
            }

            if (!batchOverlayVisible)
                compositionResetEvent.Reset();

            // stage 5: commit manifests
            foreach (var component in components)
            {
                if (componentManifests.TryGetValue(component, out var manifest) && manifest is not null)
                {
                    var componentRelativePath = getComponentRelativePath(component);
                    await updateStatusAsync(4, $"Saving manifest to `{componentRelativePath}`").ConfigureAwait(false);
                    if (component.FileObjectModel is DataBasePackedFile dbpf)
                    {
                        await dbpf.SetAsync(new ResourceKey(ResourceType.SnippetTuning, 0x80000000, manifest.TuningFullInstance), manifest).ConfigureAwait(false);
                        await dbpf.SaveAsync().ConfigureAwait(false);
                    }
                    else if (component.FileObjectModel is ZipArchive zipArchive)
                    {
                        var manifestEntry = zipArchive.CreateEntry("llamalogic.modfilemanifest.yml");
                        using var manifestEntryStream = manifestEntry.Open();
                        using var manifestEntryStreamWriter = new StreamWriter(manifestEntryStream);
                        await manifestEntryStreamWriter.WriteLineAsync(manifest.ToString()).ConfigureAwait(false);
                        await manifestEntryStreamWriter.FlushAsync().ConfigureAwait(false);
                    }
                    else
                        throw new NotSupportedException($"Unsupported component file object model type {component?.GetType().FullName}");
                    component.FileObjectModel.Dispose();
                    await updateStatusAsync(4, $"Creating scaffolding for `{componentRelativePath}`").ConfigureAwait(false);
                    var scaffolding = new ManifestedModFileScaffolding
                    {
                        ModName = name.Trim(),
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
                        var relativePath = Path.GetRelativePath(component.File.Directory!.FullName, referencedModFile.LocalAbsolutePath);
                        if (relativePath != referencedModFile.LocalAbsolutePath)
                            referencedModFile.LocalRelativePath = relativePath;
                        scaffolding.OtherModComponents.Add(referencedModFile);
                    }
                    await scaffolding.CommitForAsync(component.File, Settings).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "unhandled exception during manifest composition");
            await DialogService.ShowErrorDialogAsync("Something went wrong", "While I was writing your manifests, an unexpected error occurred. I've written it to my log file.");
        }

        if (!batchOverlayVisible)
            compositionResetEvent.Set();

        await updateStatusAsync(5, "All component manifests have been updated and scaffolding has been written").ConfigureAwait(false);
        await Task.Delay(1000).ConfigureAwait(false);

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
        Settings.PropertyChanged -= HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
        GC.SuppressFinalize(this);
    }

    async Task HandleAddFilesClickedAsync()
    {
        var modFiles = await ModFileSelector.SelectModFilesAsync();
        if (modFiles is null || modFiles.Count is 0)
            return;
        loadingText = "Reading mod files";
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

    async Task HandleAddRequiredModOnClickedAsync()
    {
        using var pbDbContext = await PbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await ModFileSelector.SelectAModFileManifestAsync(pbDbContext, DialogService) is { } manifest)
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
                if (componentsStepSelectedComponent is not { } displayedComponent || componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync("This Cannot Be Undone", "I am about to apply the component settings on screen now to every mod file you have checked in the list."))
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
        if (targetIndex is 0)
        {
            await CancelAtUserRequestAsync();
            return true;
        }
        var activeIndex = stepper?.GetActiveIndex();
        if (activeIndex is 2)
        {
            UrlProhibitiveGuidanceOpen = false;
            UrlEncouragingGuidanceOpen = false;
        }
        if (direction is StepChangeDirection.Backward)
            return false;
        if (activeIndex is 0 && components.Count == 0)
        {
            if (selectStepFile is null)
            {
                await DialogService.ShowErrorDialogAsync("Select a mod file first", "A journey of a thousand miles begins with the first step. And this is gonna be like... *much quicker* than that.");
                return true;
            }
            loadingText = $"Examining mod file";
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
                    creators = [.. Settings.DefaultCreatorsList.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
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
                            await DialogService.ShowErrorDialogAsync("Scaffolding points to missing file",
                                $"""
                                When you last updated the manifest in this mod, it had a file at this location which I failed to find. If you weren't expecting to see this message, you should probably investigate.
                                `{otherModComponent.LocalAbsolutePath}`<br /><br />
                                <iframe src="https://giphy.com/embed/6uGhT1O4sxpi8" width="480" height="259" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/awkward-pulp-fiction-john-travolta-6uGhT1O4sxpi8">via GIPHY</a></p>
                                """);
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
                        await DialogService.ShowInfoDialogAsync("So, a manifest but no scaffolding, eh?",
                            $"""
                            There are only a few reasons this might happen. In any case, you are seen.
                            1. *You are* the original creator of this mod and you don't back up your files (in which case, for shame). You're going to have to re-add all of your mod's components without the scaffolding, but I will clean-up intramod requirements for you automatically as you go.
                            2. You're a kind soul adopting an orphaned mod and the marmot smiles down upon you. If so, we do have [some potentially useful advice for you](https://plumbbuddy.app/community-services/adoption-guidance).
                            3. You're *one of those* Settingss who knows just enough to be dangerous, you lied to me during Onboarding, and now you're about to *do something **really stupid***. [Enjoy breaking things](https://youtu.be/m_F8wSZT3QY), I guess. 🤷<br /><br />
                            <iframe src="https://giphy.com/embed/xTkcEHkC6P3I5VCDpm" width="480" height="360" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/converse-xTkcEHkC6P3I5VCDpm">via GIPHY</a></p>
                            """);
                }
                UpdateComponentsStructure();
            }
            isLoading = false;
            StateHasChanged();
            if (components.Count is 0)
                return true;
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
            if (addRequiredModGuidanceOpen)
                addRequiredModGuidanceOpen = false;
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
            if (confirmationStepMessages.Any(csm => csm.severity is Severity.Warning) && !await DialogService.ShowCautionDialogAsync("Point of No Return", "You *still* have **unresolved warnings** regarding your mod's manifest. I *can* proceed with these issues if you think I'm mistaken, but *you are responsible for ignoring these warnings if I'm not*."))
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
                if (componentsStepSelectedComponents.Count == 0 || !await DialogService.ShowCautionDialogAsync("This Cannot Be Undone", "If I remove the selected components of your mod and you change your mind, you'll need to click cancel on the bottom of the window and start the whole process over again or add them back manually."))
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
        if (!await DialogService.ShowCautionDialogAsync($"Are you sure?",
            $"""
            You're about to remove your mod's requirement of {requiredMod.Name ?? "this mod"}. If you change your mind you'll have to **Cancel** and start from scratch or add it back.<br /><br />
            <iframe src="https://giphy.com/embed/qxCYGGPbQp3yj5aSsL" width="480" height="360" style="" frameBorder="0" class="giphy-embed" allowFullScreen></iframe><p><a href="https://giphy.com/gifs/thelonelyisland-season-3-i-think-you-should-leave-itysl-qxCYGGPbQp3yj5aSsL">via GIPHY</a></p>
            """))
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
        {
            Logger.LogWarning(exception, "encountered unhandled exception while selecting a folder for batch manifest update");
            await DialogService.ShowErrorDialogAsync("Whoops, That Didn't Work",
                $"""
                An error was reported back to me by the operating system:
                `{exception.GetType().Name}: {exception.Message}`<br /><br />
                I've written additional details to my log, in case they're relevant.
                """);
            return;
        }
        if (folderFromPicker is null)
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
                reportElements.Add
                (
                    $"""
                    The following files had manifests that *needed* to be updated:
                    {string.Join(Environment.NewLine, changedManifests.OrderBy(mf => mf.FullName).Select(cm => $"- {cm.FullName[commonPathLength..]}"))}
                    """
                );
            if (unchangedManifests.Count is > 0)
                reportElements.Add
                (
                    $"""
                    The following files had manifests that *did not need* to be updated:
                    {string.Join(Environment.NewLine, unchangedManifests.OrderBy(mf => mf.FullName).Select(um => $"- {um.FullName[commonPathLength..]}"))}
                    """
                );
            if (unscaffolded.Count is > 0)
                reportElements.Add
                (
                    $"""
                    The following files had no scaffolding and were therefore skipped:
                    {string.Join(Environment.NewLine, unscaffolded.OrderBy(mf => mf.FullName).Select(u => $"- {u.FullName[commonPathLength..]}"))}
                    """
                );
            var report = string.Join($"{Environment.NewLine}{Environment.NewLine}", reportElements);
            if (await DialogService.ShowQuestionDialogAsync("Would you like to copy this Batch Processing Report to your computer's clipboard?", report, false, true) ?? false)
            {
                await Clipboard.SetTextAsync(report);
                await DialogService.ShowSuccessDialogAsync("Report Successfully Copied to Clipboard", "Paste it where you like. 😏");
            }
        }
        else
            await DialogService.ShowInfoDialogAsync("Whole Lotta Noth'n", "None of the mod files examined had manifests to potentially be updated.");
    }

    Task<bool> OfferToCancelAsync() =>
        DialogService.ShowCautionDialogAsync("Are you sure you want to cancel?", "I'll discard any changes you've made to this manifest and go back to the beginning of the process.");

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        PublicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
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
                $"""
                Whoops, it looks like you used the `{componentsWithSameManifestName.Key}` **Manifest Snippet Tuning Resource Name** for each of the following mod files when they each need to be unique. You can go back to the **Components** step to fix this.
                {string.Join(Environment.NewLine, componentsWithSameManifestName.Select(component => $"* `{component.File.FullName[(commonComponentDirectory!.FullName.Length + 1)..]}`"))}
                """
            )));
        messages.AddRange(components
            .Where(component => component.FileObjectModel is DataBasePackedFile && string.IsNullOrWhiteSpace(component.ManifestResourceName))
            .Select(component =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.DiceMultiple,
                $"""
                Your `{component.File.FullName[(commonComponentDirectory!.FullName.Length + 1)..]}` mod file is a package with a **Manifest Snippet Tuning Resource Name** which you left either blank or just as white space. Since I have to have something substantive and unique there, if we continue I'm going to generate something for you. If you'd rather I didn't, you can go back to the **Components** step and fill it in for yourself.
                """
            )));
        if (string.IsNullOrWhiteSpace(name))
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                $"""
                *Your mod has no name!* You can go back to the **Details** step and type one in, and probably should because if you don't:
                * If a problem comes up and I need to discuss your mod with a Settings it will be awkward because I'll have to refer to file names when I could be using a more familiar name.
                * If your mod is ever referenced as a dependency and then a Settings doesn't have it, that conversation gets even more awkward because I have to discuss them getting it without telling them what it's called.
                """
            ));
        if (creators.Count is 0)
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.AccountSearch,
                $"""
                *You haven't specified any creators.* Look, I know stepping into the limelight can be a bit daunting... but you deserve recognition for what you've done! You can go back to the **Details** step and fill in your own name and the names of anyone who worked with you on this mod.
                """
            ));
        if (string.IsNullOrWhiteSpace(url) || !GetUrlPattern().IsMatch(url))
            messages.Add
            ((
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.CloudSearch,
                $"""
                *Your mod does not have a valid download page URL!* You can go back to the **Details** step and type one in, and probably should because if you don't:
                * If a problem comes up and I need to discuss your mod with a Settings it will be awkward because I'll have no place to send them on the web if they need to download a fresh copy or an update.
                * If your mod is ever referenced as a dependency and then a Settings doesn't have it or needs to update it, that conversation gets even more awkward because... again... what do I say? Google it? Come on, don't do that to me... 😔
                """
            ));
        messages.AddRange(requiredMods
            .Select((requiredMod, index) => (requiredMod, index))
            .Where(t => string.IsNullOrWhiteSpace(t.requiredMod.Name))
            .Select(t =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                $"""
                *Your {(t.index + 1).Ordinalize()} required mod has no name!* You can go back to the **Requirements** step and type one in, and probably should because if you don't and a problem comes up and I need to discuss your mod's requirements with a Settings, it's gonna be very vague. *Did you notice how I just had to tell you which requirement has this problem **with a number?***
                """
            )));
        messages.AddRange(requiredMods
            .Where(requiredMod => !string.IsNullOrWhiteSpace(requiredMod.Name) && (string.IsNullOrWhiteSpace(requiredMod.Url) || !Uri.TryCreate(requiredMod.Url, UriKind.Absolute, out _)))
            .Select(requiredMod =>
            (
                Severity.Warning,
                (string?)MaterialDesignIcons.Normal.TagSearch,
                $"""
                *Your **{requiredMod.Name}** required mod doesn't have a valid download page URL!* You can go back to the **Requirements** step and type one in, and probably should because if you don't and a problem comes up and I need to discuss your mod's requirement with a Settings, it will be awkward because the only place to send them on the web to try to find a fresh copy of the requirement will be your mod's download page. Not the best user experience.
                """
            )));
        if (requiredPacks.Count is > 0 && string.IsNullOrWhiteSpace(electronicArtsPromoCode))
            messages.Add
            ((
                Severity.Normal,
                (string?)MaterialDesignIcons.Normal.AccountCash,
                $"""
                Your mod requires packs which some Settings may not have, but might purchase specifically to use with *your mod*, and yet, you have not given me an **EA Promo Code**. If you do have one, you really shouldn't be leaving money on the table like this, friend. Consider doing yourself a solid, heading back to the **Requirements** step, and filling in that code.<br />
                If you don't have one, but you're interested in getting a commission for all the packs your awesome mod is about to help EA sell, please allow me to [direct you to where that journey begins](https://creatornetwork.ea.com/).
                """
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
    }

    void UpdateComponentsStructure()
    {
        var previousCommonComponentDirectoryFullName = commonComponentDirectory is not null
            ? Path.GetFullPath(commonComponentDirectory.FullName)
            : string.Empty;
        var componentPathsSplitBySegment = components
            .Select(component => component.File.FullName)
            .Select(path => path!.Split(Path.DirectorySeparatorChar))
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
