namespace PlumbBuddy.Services;

public class Catalog :
    ICatalog
{
    public Catalog(ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IModsDirectoryCataloger modsDirectoryCataloger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.modsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        QueryManifests();
    }

    ~Catalog() =>
        Dispose(false);

    bool isDisposed;
    IReadOnlyDictionary<CatalogModKey, IReadOnlyList<CatalogModValue>> mods =
        new Dictionary<CatalogModKey, IReadOnlyList<CatalogModValue>>().ToImmutableDictionary();
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    string modsSearchText = string.Empty;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    CatalogModKey? selectedModKey;
    readonly ISettings settings;

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
            isDisposed = true;
        }
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State)
            && modsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            QueryManifests();
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    void QueryManifests() =>
        Task.Run(QueryManifestsAsync);

    async Task QueryManifestsAsync()
    {
        var userDataFolderPath = settings.UserDataFolderPath;
        var mods = new Dictionary<CatalogModKey, List<CatalogModValue>>();
        await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        foreach (var activeManifest in await pbDbContext.ModFileManifestHashes
            .SelectMany(mfmh => mfmh.ManifestsByCalculation!)
            .Where(mfm => mfm.ModFileHash.ModFiles.Any())
            .Include(mfm => mfm.ModFileHash!)
                .ThenInclude(mfh => mfh.ModFiles)
            .Include(mfm => mfm.Creators!)
            .Include(mfm => mfm.ElectronicArtsPromoCode)
            .Include(mfm => mfm.Exclusivities)
            .Include(mfm => mfm.Features)
            .Include(mfm => mfm.HashResourceKeys)
            .Include(mfm => mfm.IncompatiblePacks)
            .Include(mfm => mfm.CalculatedModFileManifestHash!)
                .ThenInclude(mfmh => mfmh.Dependents!)
            .Include(mfm => mfm.RequiredMods!)
                .ThenInclude(rm => rm.Creators)
            .Include(mfm => mfm.RequiredMods!)
                .ThenInclude(rm => rm.Hashes!)
                    .ThenInclude(mfmh => mfmh.ManifestsByCalculation)
            .Include(mfm => mfm.RequiredMods!)
                .ThenInclude(rm => rm.RequiredFeatures)
            .Include(mfm => mfm.RequiredMods!)
                .ThenInclude(rm => rm.RequirementIdentifier)
            .Include(mfm => mfm.RequiredPacks)
            .Include(mfm => mfm.SubsumedHashes)
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
            values.Add(new
            (
                activeManifest.ToModel(),
                activeManifest.ModFileHash.ModFiles.Select(mf => new FileInfo(Path.Combine(userDataFolderPath, "Mods", mf.Path))).ToList().AsReadOnly(),
                (activeManifest.RequiredMods ?? Enumerable.Empty<RequiredMod>()).Select(rm => new CatalogModKey(rm.Name, rm.Creators?.Select(c => c.Name).Order().Humanize(), rm.Url)).Distinct().Except([key]).ToList().AsReadOnly(),
                (activeManifest.CalculatedModFileManifestHash?.Dependents ?? Enumerable.Empty<RequiredMod>()).Select(d => d.ModFileManifest).Where(mfm => mfm is not null).Cast<ModFileManifest>().Select(mfm => new CatalogModKey(mfm.Name, mfm.Creators?.Select(c => c.Name).Order().Humanize(), mfm.Url)).Distinct().Except([key]).ToList().AsReadOnly()
            ));
        }
        Mods = mods.ToImmutableDictionary(kv => kv.Key, kv => (IReadOnlyList<CatalogModValue>)kv.Value.AsReadOnly());
    }
}
