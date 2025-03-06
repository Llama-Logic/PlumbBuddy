namespace PlumbBuddy.Services;

public sealed class Parlay :
    IParlay
{
    static readonly ImmutableArray<CultureInfo> maxisLocales =
    [
        new ("en-US"),
        new ("zh-CN"),
        new ("zh-TW"),
        new ("cs"),
        new ("da"),
        new ("nl"),
        new ("fi"),
        new ("fr-FR"),
        new ("de"),
        new ("it"),
        new ("ja"),
        new ("ko"),
        new ("nb"),
        new ("pl"),
        new ("pt-BR"),
        new ("ru"),
        new ("es-ES"),
        new ("sv")
    ];
    static readonly ImmutableDictionary<string, CultureInfo> maxisLocaleByNeutralLocaleName = maxisLocales
        .GroupBy(ci => ci.GetNeutralCultureInfo().Name)
        .ToImmutableDictionary(g => g.Key, g => g.First());

    public Parlay(ISettings settings, ILogger<Parlay> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.settings = settings;
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        packages = [];
        publicOrigins = packages.ToList().AsReadOnly();
        refreshOriginsLock = new();
        stringTableEntriesLock = new();
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        RefreshOrigins();
    }

    ~Parlay() =>
        Dispose(false);

    string? creators;
    ParlayStringTableEntry? editingEntry;
    string editingEntryTranslation = string.Empty;
    string entrySearchText = string.Empty;
    bool isDisposed;
    readonly ILogger<Parlay> logger;
    string? messageFromCreators;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    FileInfo? originalPackageFile;
    readonly List<ParlayPackage> packages;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    IReadOnlyList<ParlayPackage> publicOrigins;
    ReadOnlyObservableCollection<ParlayStringTableEntry>? publicStringTableEntries;
    readonly AsyncLock refreshOriginsLock;
    CultureInfo? repurposingMaxisLocale;
    readonly AsyncLock stringTableEntriesLock;
    ParlayPackage? selectedOrigin;
    readonly ISettings settings;
    ParlayStringTable? shownStringTable;
    readonly ISuperSnacks superSnacks;
    ObservableCollection<ParlayStringTableEntry>? stringTableEntries;
    ParlayLocale? translationLocale;
    FileInfo? translationPackageFile;
    ResourceKey translationStringTableKey;
    Uri? translationSubmissionUrl;

    public string? Creators
    {
        get => creators;
        private set
        {
            if (creators == value)
                return;
            creators = value;
            OnPropertyChanged();
        }
    }

    public ParlayStringTableEntry? EditingEntry
    {
        get => editingEntry;
        set
        {
            if (editingEntry?.Hash == value?.Hash)
                return;
            editingEntry = value;
            OnPropertyChanged();
        }
    }

    public string EditingEntryTranslation
    {
        get => editingEntryTranslation;
        set
        {
            if (editingEntryTranslation == value)
                return;
            editingEntryTranslation = value;
            OnPropertyChanged();
        }
    }

    public string EntrySearchText
    {
        get => entrySearchText;
        set
        {
            if (entrySearchText == value)
                return;
            entrySearchText = value;
            OnPropertyChanged();
        }
    }

    public string? MessageFromCreators
    {
        get => messageFromCreators;
        private set
        {
            if (messageFromCreators == value)
                return;
            messageFromCreators = value;
            OnPropertyChanged();
        }
    }

    public FileInfo? OriginalPackageFile
    {
        get => originalPackageFile;
        set
        {
            if (originalPackageFile?.FullName == value?.FullName)
                return;
            originalPackageFile = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<ParlayPackage> Packages
    {
        get => publicOrigins;
        private set
        {
            publicOrigins = value;
            OnPropertyChanged();
        }
    }

    public ParlayPackage? SelectedPackage
    {
        get => selectedOrigin;
        set
        {
            if (selectedOrigin?.ModFilePath == value?.ModFilePath)
                return;
            selectedOrigin = value;
            OnPropertyChanged();
            ShownStringTable = null;
            OriginalPackageFile = SelectedPackage?.ModFilePath is { } selectedPackagePath
                ? new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", selectedPackagePath))
                : null;
            RefreshCreatorProperties();
        }
    }

    public ParlayStringTable? ShownStringTable
    {
        get => shownStringTable;
        set
        {
            shownStringTable = value;
            OnPropertyChanged();
            RefreshStringTableEntries();
        }
    }

    public ReadOnlyObservableCollection<ParlayStringTableEntry>? StringTableEntries
    {
        get => publicStringTableEntries;
        private set
        {
            publicStringTableEntries = value;
            OnPropertyChanged();
        }
    }

    public ParlayLocale? TranslationLocale
    {
        get => translationLocale;
        set
        {
            if (translationLocale?.Locale.Name == value?.Locale.Name)
                return;
            translationLocale = value;
            OnPropertyChanged();
            RefreshStringTableEntries();
        }
    }

    public ImmutableArray<ParlayLocale> TranslationLocales
    {
        get
        {
            var coveredNeutralCultureNames = maxisLocales
                .Select(ci => ci.Name)
                .Distinct()
                .ToImmutableHashSet();
            return maxisLocales
                .Concat
                (
                    CultureInfo
                        .GetCultures(CultureTypes.SpecificCultures)
                        .Where(ci => !coveredNeutralCultureNames.Contains(ci.Name))
                        .OrderBy(ci => ci.NativeName)
                )
                .Select(ci => new ParlayLocale(ci))
                .ToImmutableArray();
        }
    }

    public FileInfo? TranslationPackageFile
    {
        get => translationPackageFile;
        set
        {
            if (translationPackageFile?.FullName == value?.FullName)
                return;
            translationPackageFile = value;
            OnPropertyChanged();
        }
    }

    public Uri? TranslationSubmissionUrl
    {
        get => translationSubmissionUrl;
        private set
        {
            if (translationSubmissionUrl == value)
                return;
            translationSubmissionUrl = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing && !isDisposed)
        {
            modsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
            isDisposed = true;
        }
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State)
            && modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            RefreshOrigins();
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ParlayName))
            SaveTranslation();
    }

    public async Task<int> MergeStringTableAsync(FileInfo fromPackageFile)
    {
        if (translationPackageFile is null
            || stringTableEntries is null)
            return 0;
        ArgumentNullException.ThrowIfNull(fromPackageFile);
        using var fromPackage = await DataBasePackedFile.FromPathAsync(fromPackageFile.FullName).ConfigureAwait(false);
        if (!await fromPackage.ContainsKeyAsync(translationStringTableKey).ConfigureAwait(false))
            return 0;
        var importStbl = await fromPackage.GetModelAsync<StringTableModel>(translationStringTableKey).ConfigureAwait(false);
        var importCount = 0;
        foreach (var stringTableEntry in stringTableEntries)
        {
            var importString = importStbl.Get(stringTableEntry.Hash);
            if (!string.IsNullOrWhiteSpace(importString))
            {
                stringTableEntry.Translation = importString;
                ++importCount;
            }
        }
        return importCount;
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    void RefreshCreatorProperties()
    {
        var reset = true;
        try
        {
            if (SelectedPackage is not { } selectedPackage)
                return;
            Creators = selectedPackage.ManifestedCreators;
            MessageFromCreators = selectedPackage.ManifestedMessageFromCreators;
            TranslationSubmissionUrl = selectedPackage.ManifestedTranslationSubmissionUrl;
            reset = false;
        }
        finally
        {
            if (reset)
            {
                Creators = null;
                MessageFromCreators = null;
                TranslationSubmissionUrl = null;
            }
        }
    }

    void RefreshOrigins() =>
        _ = Task.Run(RefreshOriginsAsync);

    async Task RefreshOriginsAsync()
    {
        IDisposable? refreshOriginsLockHeld = null;
        try
        {
            try
            {
                refreshOriginsLockHeld = await refreshOriginsLock.LockAsync(new CancellationToken(true)).ConfigureAwait(false);
                if (refreshOriginsLockHeld is null)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            var packagesToRemove = packages
                .ToDictionary(p => p.ModFilePath);
            var signedStringTableResourceType = unchecked((int)(uint)ResourceType.StringTable);
            await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
            var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await foreach (var packageRecord in pbDbContext.ModFiles
                .Where(mf => !mf.Path.EndsWith(".l10n.package")
                    && mf.ModFileHash!.Resources!.Any(r => r.KeyType == signedStringTableResourceType))
                .Select(mf => new
                {
                    mf.Path,
                    StringTableKeys = mf.ModFileHash!.Resources!
                        .Where(r => r.KeyType == signedStringTableResourceType)
                        .Select(r => new
                        {
                            r.KeyGroup,
                            r.KeyFullInstance
                        })
                        .ToList(),
                    ModFileManifests = mf.ModFileHash!.ModFileManifests!.Select(mfm => new
                    {
                        mfm.Name,
                        Creators = mfm.Creators!.Select(mc => mc.Name).ToList(),
                        mfm.Version,
                        mfm.MessageToTranslators,
                        mfm.TranslationSubmissionUrl,
                        RepurposedLanguages = mfm.RepurposedLanguages!.Select(rl => new
                        {
                            rl.ActualLocale,
                            rl.GameLocale
                        }).ToList()
                    }).ToList()
                })
                .AsAsyncEnumerable())
            {
                var path = packageRecord.Path;
                if (packagesToRemove.TryGetValue(path, out var existingPackage))
                {
                    packages.Remove(existingPackage);
                    packagesToRemove.Remove(path);
                }
                var manifestedName = packageRecord.ModFileManifests
                    .Where(mfm => !string.IsNullOrWhiteSpace(mfm.Name))
                    .GroupBy(mfm => mfm.Name)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()
                    ?.Key;
                var manifestedCreators = packageRecord.ModFileManifests
                    .SelectMany(mfm => mfm.Creators)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .ToImmutableArray() is { Length: > 0 } creators
                    ? creators.Humanize()
                    : null;
                var manifestedVersion = packageRecord.ModFileManifests
                    .Where(mfm => !string.IsNullOrEmpty(mfm.Version))
                    .GroupBy(mfm => mfm.Version)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()
                    ?.Key;
                var messagesFromCreators = packageRecord.ModFileManifests
                    .Where(mfm => !string.IsNullOrWhiteSpace(mfm.MessageToTranslators))
                    .Select(mfm => mfm.MessageToTranslators)
                    .ToImmutableArray();
                var manifestedMessageFromCreators = messagesFromCreators.Length is 0
                    ? null
                    : string.Join($"{Environment.NewLine}{Environment.NewLine}", messagesFromCreators);
                var manifestedTranslationSubmissionUrl = packageRecord.ModFileManifests
                    .FirstOrDefault(mfm => mfm.TranslationSubmissionUrl is not null)
                    ?.TranslationSubmissionUrl;
                packages.Add(new(path, manifestedName, manifestedCreators, manifestedVersion, manifestedMessageFromCreators, manifestedTranslationSubmissionUrl, packageRecord.StringTableKeys
                    .Select(stk =>
                    {
                        var stringTableKey = new ResourceKey
                        (
                            ResourceType.StringTable,
                            unchecked((uint)stk.KeyGroup),
                            unchecked((ulong)stk.KeyFullInstance)
                        );
                        CultureInfo? locale = null;
                        try
                        {
                            locale = SmartSimUtilities.GetStringTableLocale(stringTableKey);
                        }
                        catch
                        {
                        }
                        if (locale is not null)
                            foreach (var repurposedLangauge in packageRecord.ModFileManifests.SelectMany(mfm => mfm.RepurposedLanguages))
                                if (locale.Name.Equals(repurposedLangauge.GameLocale, StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        locale = CultureInfo.GetCultureInfo(repurposedLangauge.ActualLocale);
                                        break;
                                    }
                                    catch
                                    {
                                    }
                                }
                        return (stringTableKey, locale);
                    })
                    .Where(stkl => stkl.locale is not null)
                    .Select(stkl => new ParlayStringTable(stkl.stringTableKey, stkl.locale!))
                    .ToImmutableArray()));
            }
            foreach (var remainingPackage in packagesToRemove.Values)
                packages.Remove(remainingPackage);
            Packages = packages.ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
        finally
        {
            refreshOriginsLockHeld?.Dispose();
        }
    }

    void RefreshStringTableEntries() =>
        _ = Task.Run(RefreshStringTableEntriesAsync);

    async Task RefreshStringTableEntriesAsync()
    {
        var maybeNullSelectedPackagePath = SelectedPackage?.ModFilePath;
        var maybeNullOriginalStringTableKey = ShownStringTable?.StringTableKey;
        var maybeNullTranslationLocale = TranslationLocale?.Locale;
        using var stringTableEntriesLockHeld = await stringTableEntriesLock.LockAsync().ConfigureAwait(false);
        if (maybeNullSelectedPackagePath is not { } selectedPackagePath
            || maybeNullOriginalStringTableKey is not { } originalStringTableKey)
        {
            stringTableEntries = null;
            StringTableEntries = null;
            return;
        }
        try
        {
            if (this.originalPackageFile is not { } originalPackageFile)
                return;
            TranslationPackageFile =
                maybeNullTranslationLocale is { } translationLocaleForFileName
                ? new FileInfo(Path.Combine(originalPackageFile.Directory!.FullName, $"!{originalPackageFile.Name[..^originalPackageFile.Extension.Length]}.{translationLocaleForFileName.Name}.l10n.package"))
                : null;
            using var originalPackage = await DataBasePackedFile.FromPathAsync(originalPackageFile.FullName).ConfigureAwait(false);
            var originalStbl = await originalPackage.GetModelAsync<StringTableModel>(originalStringTableKey).ConfigureAwait(false);
            StringTableModel? translationStbl = null;
            if (maybeNullTranslationLocale is { } translationLocale)
            {
                repurposingMaxisLocale = null;
                if (maxisLocales.Any(ml => ml.Name.Equals(translationLocale.Name, StringComparison.OrdinalIgnoreCase)))
                    translationStringTableKey = SmartSimUtilities.GetStringTableResourceKey(translationLocale, originalStringTableKey.Group, originalStringTableKey.FullInstance);
                else
                {
                    repurposingMaxisLocale = maxisLocaleByNeutralLocaleName.TryGetValue(translationLocale.GetNeutralCultureInfo().Name, out var ml)
                        ? ml
                        : CultureInfo.GetCultureInfo("en-US");
                    translationStringTableKey = SmartSimUtilities.GetStringTableResourceKey(repurposingMaxisLocale, originalStringTableKey.Group, originalStringTableKey.FullInstance);
                }
                if (TranslationPackageFile is { } translationPackageFile)
                {
                    translationPackageFile.Refresh();
                    if (translationPackageFile.Exists)
                    {
                        using var translationPackage = await DataBasePackedFile.FromPathAsync(translationPackageFile.FullName).ConfigureAwait(false);
                        if (await translationPackage.ContainsKeyAsync(translationStringTableKey))
                            translationStbl = await translationPackage.GetModelAsync<StringTableModel>(translationStringTableKey).ConfigureAwait(false);
                    }
                }
            }
            stringTableEntries = [..originalStbl.KeyHashes.Select(hash => new ParlayStringTableEntry(this, hash, originalStbl.Get(hash), translationStbl?.Get(hash) ?? string.Empty))];
            StringTableEntries = new(stringTableEntries);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unexpected exception encountered while processing {ModFilePath}::{StringTableKey}", selectedPackagePath, originalStringTableKey);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(
                """
                <h3>Whoops!</h3>
                I ran into a problem trying to read:<br />
                <strong>{0}::{1}</strong><br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{2}: {3}</span><br />
                <br />
                More detailed technical information is available in my log.
                """, selectedPackagePath, originalStringTableKey, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
            {
                options.RequireInteraction = true;
                options.Icon = MaterialDesignIcons.Normal.MessageAlert;
            });
        }
    }

    void SaveTranslation() =>
        _ = Task.Run(SaveTranslationAsync);

    public async Task SaveTranslationAsync()
    {
        var maybeNullOriginalPackageFile = originalPackageFile;
        var maybeNullOriginalStringTableKey = ShownStringTable?.StringTableKey;
        var maybeNullTranslationPackageFile = translationPackageFile;
        var maybeNullTranslationLocale = TranslationLocale?.Locale;
        DataBasePackedFile? translationPackage = null;
        try
        {
            var maybeNullStringTableEntries = this.stringTableEntries?.ToImmutableDictionary(ste => ste.Hash, ste => (original: ste.Original, translation: ste.Translation));
            using var stringTableEntriesLockHeld = await stringTableEntriesLock.LockAsync().ConfigureAwait(false);
            if (maybeNullOriginalPackageFile is not { } originalPackageFile
                || maybeNullOriginalStringTableKey is not { } originalStringTableKey
                || maybeNullTranslationPackageFile is not { } translationPackageFile
                || maybeNullTranslationLocale is not { } translationLocale
                || maybeNullStringTableEntries is not { } stringTableEntries)
                return;
            using var originalPackage = await DataBasePackedFile.FromPathAsync(originalPackageFile.FullName).ConfigureAwait(false);
            var originalPackageManifests = await ModFileManifestModel.GetModFileManifestsAsync(originalPackage).ConfigureAwait(false);
            translationPackageFile.Refresh();
            if (translationPackageFile.Exists)
                translationPackage = await DataBasePackedFile.FromPathAsync(translationPackageFile.FullName, forReadOnly: false).ConfigureAwait(false);
            else
#pragma warning disable CA2000 // Dispose objects before losing scope
                translationPackage = new();
#pragma warning restore CA2000 // Dispose objects before losing scope
            var translationPackageManifests = await ModFileManifestModel.GetModFileManifestAsync(translationPackage).ConfigureAwait(false);
            await ModFileManifestModel.DeleteModFileManifestsAsync(translationPackage).ConfigureAwait(false);
            var tuningName = $"translationManifest_{Guid.NewGuid():n}";
            var translationPackageManifest = new ModFileManifestModel
            {
                Name = $"{originalPackageManifests.Values.Where(m => !string.IsNullOrWhiteSpace(m.Name)).GroupBy(m => m.Name).OrderByDescending(g => g.Key.Count()).FirstOrDefault()?.Key ?? originalPackageFile.Name} - {translationLocale.NativeName}",
                TuningFullInstance = Fnv64.SetHighBit(Fnv64.GetHash(tuningName)),
                TuningName = tuningName
            };
            if (!string.IsNullOrWhiteSpace(settings.ParlayName))
                translationPackageManifest.Translators.Add(new ModFileManifestModelTranslator
                {
                    Culture = translationLocale,
                    Name = settings.ParlayName
                });
            var repurposedLanguages = new Dictionary<string, string>();
            if (translationPackageManifests is not null)
                foreach (var repurposedLanguage in translationPackageManifests.RepurposedLanguages)
                    repurposedLanguages.TryAdd(repurposedLanguage.GameLocale, repurposedLanguage.ActualLocale);
            if (repurposingMaxisLocale is not null
                && !repurposedLanguages.TryAdd(repurposingMaxisLocale.Name, translationLocale.Name))
                repurposedLanguages[repurposingMaxisLocale.Name] = translationLocale.Name;
            var translationStbl = new StringTableModel();
            foreach (var (hash, originalAndTranslation) in stringTableEntries)
            {
                var (_, translation) = originalAndTranslation;
                if (!string.IsNullOrWhiteSpace(translation))
                    translationStbl.Set(hash, translation);
            }
            await translationPackage.SetAsync(translationStringTableKey, translationStbl).ConfigureAwait(false);
            translationPackageManifest.HashResourceKeys.Clear();
            foreach (var key in await translationPackage.GetKeysAsync().ConfigureAwait(false))
                translationPackageManifest.HashResourceKeys.Add(key);
            translationPackageManifest.Hash = await ModFileManifestModel.GetModFileHashAsync(translationPackage, translationPackageManifest.HashResourceKeys).ConfigureAwait(false);
            translationPackageManifest.RepurposedLanguages.Clear();
            foreach (var (gameLocale, actualLocale) in repurposedLanguages)
                translationPackageManifest.RepurposedLanguages.Add(new()
                {
                    ActualLocale = actualLocale,
                    GameLocale = gameLocale
                });
            translationPackageManifest.RequiredMods.Clear();
            foreach (var originalPackageManifest in originalPackageManifests.Values)
            {
                var requiredMod = new ModFileManifestModelRequiredMod
                {
                    IgnoreIfHashAvailable = [],
                    IgnoreIfHashUnavailable = [],
                    Name = originalPackageManifest.Name,
                    Url = originalPackageManifest.Url,
                    Version = originalPackageManifest.Version
                };
                foreach (var name in originalPackageManifest.Creators)
                    requiredMod.Creators.Add(name);
                requiredMod.Hashes.Add(originalPackageManifest.Hash);
                translationPackageManifest.RequiredMods.Add(requiredMod);
            }
            await translationPackage.SetAsync(new ResourceKey(ResourceType.SnippetTuning, 0x80000000, translationPackageManifest.TuningFullInstance), translationPackageManifest).ConfigureAwait(false);
            if (translationPackage.CanSaveInPlace)
                await translationPackage.SaveAsync().ConfigureAwait(false);
            else
                await translationPackage.SaveAsAsync(translationPackageFile.FullName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unexpected exception encountered while saving translation of {ModFilePath} from {OriginalLocaleKey} to {TranslationLocaleKey} in {TranslationFilePath}", maybeNullOriginalPackageFile, maybeNullOriginalStringTableKey, maybeNullTranslationLocale, maybeNullTranslationPackageFile);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(
                """
                <h3>Whoops!</h3>
                I ran into a problem trying to write {0} locale to:<br />
                <strong>{1}</strong><br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{2}: {3}</span><br />
                <br />
                More detailed technical information is available in my log.
                """, maybeNullTranslationLocale, maybeNullTranslationPackageFile?.FullName, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.MessageAlert;
                });
        }
        finally
        {
            if (translationPackage is not null)
                await translationPackage.DisposeAsync().ConfigureAwait(false);
        }
    }
}
