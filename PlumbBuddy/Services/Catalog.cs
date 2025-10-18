namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1724: Type names should not match namespaces")]
public partial class Catalog :
    ICatalog
{
    public Catalog(ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IModsDirectoryCataloger modsDirectoryCataloger, IGameResourceCataloger gameResourceCataloger, IPublicCatalogs publicCatalogs)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(gameResourceCataloger);
        ArgumentNullException.ThrowIfNull(publicCatalogs);
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        this.gameResourceCataloger = gameResourceCataloger;
        this.publicCatalogs = publicCatalogs;
        this.publicCatalogs.PropertyChanged += HandlePublicCatalogsPropertyChanged;
        packIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        PackIcons = packIcons.AsReadOnly();
        QueryManifests();
    }

    ~Catalog() =>
        Dispose(false);

    readonly IGameResourceCataloger gameResourceCataloger;
    bool isDisposed;
    bool isQuerying;
    IReadOnlyDictionary<CatalogModKey, IReadOnlyList<CatalogModValue>> mods =
        new Dictionary<CatalogModKey, IReadOnlyList<CatalogModValue>>().ToImmutableDictionary();
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    string modsSearchText = string.Empty;
    readonly Dictionary<string, string> packIcons;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPublicCatalogs publicCatalogs;
    CatalogModKey? selectedModKey;
    readonly ISettings settings;

    public bool IsQuerying
    {
        get => isQuerying;
        private set
        {
            if (isQuerying == value)
                return;
            isQuerying = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyDictionary<CatalogModKey, IReadOnlyList<CatalogModValue>> Mods
    {
        get => mods;
        set
        {
            mods = value;
            OnPropertyChanged();
        }
    }

    public string ModsSearchText
    {
        get => modsSearchText;
        set
        {
            if (modsSearchText == value)
                return;
            modsSearchText = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyDictionary<string, string> PackIcons { get; }

    public CatalogModKey? SelectedModKey
    {
        get => selectedModKey;
        set
        {
            if (selectedModKey == value)
                return;
            selectedModKey = value;
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
            publicCatalogs.PropertyChanged -= HandlePublicCatalogsPropertyChanged;
            isDisposed = true;
        }
    }

    async Task EnsurePackIconsAsync(ModFileManifestModel model)
    {
        foreach (var packCode in model.RequiredPacks.Concat(model.IncompatiblePacks).Concat(model.RecommendedPacks.Select(recommendedPack => recommendedPack.PackCode)).Distinct())
        {
            if (packIcons.ContainsKey(packCode))
                continue;
            packIcons.Add(packCode, $"data:image/png;base64,{Convert.ToBase64String((await gameResourceCataloger.GetPackIcon64Async(packCode).ConfigureAwait(false)).Span)}");
        }
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State)
            && modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            QueryManifests();
    }

    void HandlePublicCatalogsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPublicCatalogs.PackCatalog))
            QueryManifests();
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    void QueryManifests() =>
        Task.Run(QueryManifestsAsync);

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task QueryManifestsAsync()
    {
        IsQuerying = true;
        var userDataFolderPath = settings.UserDataFolderPath;
        var mods = new Dictionary<CatalogModKey, List<CatalogModValue>>();
        await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
        await gameResourceCataloger.WaitForIdleAsync().ConfigureAwait(false);
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        foreach (var activeManifest in await pbDbContext.ModFileManifestHashes
            .SelectMany(mfmh => mfmh.ManifestsByCalculation!)
            .Where(mfm => mfm.ModFileHash.ModFiles.Any(mf => mf.FoundAbsent == null))
            .Include(mfm => mfm.ModFileHash!)
                .ThenInclude(mfh => mfh.ModFiles)
            .Include(mfm => mfm.Creators!)
            .Include(mfm => mfm.ElectronicArtsPromoCode)
            .Include(mfm => mfm.Exclusivities)
            .Include(mfm => mfm.Features)
            .Include(mfm => mfm.HashResourceKeys)
            .Include(mfm => mfm.IncompatiblePacks)
            .Include(mfm => mfm.CalculatedModFileManifestHash)
                .ThenInclude(mfmh => mfmh.Dependents)
            .Include(mfm => mfm.RecommendedPacks)
                .ThenInclude(rp => rp.PackCode)
            .Include(mfm => mfm.RequiredMods)
                .ThenInclude(rm => rm.Creators)
            .Include(mfm => mfm.RequiredMods)
                .ThenInclude(rm => rm.Hashes)
                    .ThenInclude(mfmh => mfmh.ManifestsByCalculation)
            .Include(mfm => mfm.RequiredMods)
                .ThenInclude(rm => rm.RequiredFeatures)
            .Include(mfm => mfm.RequiredMods)
                .ThenInclude(rm => rm.RequirementIdentifier)
            .Include(mfm => mfm.RequiredPacks)
            .Include(mfm => mfm.SubsumedHashes)
                .ThenInclude(sh => sh.Dependents)
            .Include(mfm => mfm.Translators)
            .ToListAsync()
            .ConfigureAwait(false))
        {
            var key = new CatalogModKey(activeManifest.Name, activeManifest.Creators?.Select(creator => creator.Name).Order().Humanize(), activeManifest.Url);
            if (!mods.TryGetValue(key, out var values))
            {
                values = [];
                mods.Add(key, values);
            }
            var model = activeManifest.ToModel();
            await EnsurePackIconsAsync(model).ConfigureAwait(false);
            values.Add(new
            (
                model,
                activeManifest.ModFileHash.ModFiles.Where(mf => mf.FoundAbsent == null).Select(mf => new FileInfo(Path.Combine(userDataFolderPath, "Mods", mf.Path))).ToList().AsReadOnly(),
                (activeManifest.RequiredMods ?? Enumerable.Empty<RequiredMod>()).Select(rm => new CatalogModKey(rm.Name, rm.Creators?.Select(c => c.Name).Order().Humanize(), rm.Url)).Distinct().Except([key]).ToList().AsReadOnly(),
                (activeManifest.CalculatedModFileManifestHash?.Dependents ?? Enumerable.Empty<RequiredMod>()).Concat(activeManifest.SubsumedHashes.SelectMany(sh => sh.Dependents) ?? []).Select(d => d.ModFileManifest).Where(mfm => mfm is not null).Cast<ModFileManifest>().Select(mfm => new CatalogModKey(mfm.Name, mfm.Creators?.Select(c => c.Name).Order().Humanize(), mfm.Url)).Distinct().Except([key]).ToList().AsReadOnly()
            ));
        }
        Mods = mods.ToImmutableDictionary(kv => kv.Key, kv => (IReadOnlyList<CatalogModValue>)kv.Value.AsReadOnly());
        IsQuerying = false;
    }
}
