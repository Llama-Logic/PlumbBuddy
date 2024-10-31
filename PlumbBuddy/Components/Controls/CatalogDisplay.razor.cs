namespace PlumbBuddy.Components.Controls;

partial class CatalogDisplay
{
    record ModValue(ModFileManifestModel Manifest, List<FileInfo> Files, List<ModKey> Dependencies, List<ModKey> Dependents);
    record ModKey(string Name, string? Creators, Uri? Url);

    string dependenciesSearchText = string.Empty;
    string dependentsSearchText = string.Empty;
    string filesSearchText = string.Empty;
    IReadOnlyDictionary<ModKey, IReadOnlyList<(ModFileManifestModel manifest, IReadOnlyList<FileInfo> files, IReadOnlyList<ModKey> dependencies, IReadOnlyList<ModKey> dependents)>>? mods;
    string modsFolderPath = string.Empty;
    string modsSearchText = string.Empty;
    ModKey? selectedModKey;

    string ModsSearchText
    {
        get => modsSearchText;
        set
        {
            modsSearchText = value;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
    }

    IReadOnlyList<BreadcrumbItem> GetModFileBreadcrumbs(FileInfo modFile)
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        var segments = modFile.FullName[modsFolderPath.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        for (var i = 0; i < segments.Length - 1; ++i)
            breadcrumbs.Add(new(segments[i], null, icon: MaterialDesignIcons.Normal.Folder));
        breadcrumbs.Add(new(segments[^1], null, icon: modFile.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase) ? MaterialDesignIcons.Normal.SourceBranch : MaterialDesignIcons.Normal.PackageVariantClosed));
        return [..breadcrumbs];
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State) && ModsDirectoryCataloger.State is ModsDirectoryCatalogerState.Idle)
            _ = Task.Run(QueryManifestsAsync);
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.UserDataFolderPath))
            StaticDispatcher.Dispatch(() => modsFolderPath = Path.Combine(Player.UserDataFolderPath));
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.InstalledPackCodes))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    bool IncludeDependency(ModKey key)
    {
        if (string.IsNullOrWhiteSpace(dependenciesSearchText))
            return true;
        if (key.Name.Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(dependenciesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        return false;
    }

    bool IncludeDependent(ModKey key)
    {
        if (string.IsNullOrWhiteSpace(dependentsSearchText))
            return true;
        if (key.Name.Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(dependentsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        return false;
    }

    bool IncludeFile(FileInfo fileInfo)
    {
        if (string.IsNullOrWhiteSpace(filesSearchText))
            return true;
        if (fileInfo.FullName[(modsFolderPath.Length + 1)..].Contains(filesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    bool IncludeMod(KeyValuePair<ModKey, IReadOnlyList<(ModFileManifestModel manifest, IReadOnlyList<FileInfo> files, IReadOnlyList<ModKey> dependencies, IReadOnlyList<ModKey> dependents)>> kv)
    {
        var (key, value) = kv;
        if (string.IsNullOrWhiteSpace(modsSearchText))
            return true;
        if (key.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (key.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (key.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        foreach (var (manifest, files, dependencies, dependents) in value)
        {
            if (manifest.RequiredPacks.Any(rp => rp.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (manifest.IncompatiblePacks.Any(ip => ip.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (files.Any(file => file.FullName[modsFolderPath.Length..].Contains(modsSearchText, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (dependencies.Any(dependency => dependency.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) || (dependency.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false) || (dependency.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)))
                return true;
            if (dependents.Any(dependent => dependent.Name.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) || (dependent.Creators?.Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false) || (dependent.Url?.ToString().Contains(modsSearchText, StringComparison.OrdinalIgnoreCase) ?? false)))
                return true;
        }
        return false;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
        modsFolderPath = Path.Combine(Player.UserDataFolderPath, "Mods");
        _ = Task.Run(QueryManifestsAsync);
    }

    async Task QueryManifestsAsync()
    {
        var userDataFolderPath = Player.UserDataFolderPath;
        var mods = new Dictionary<ModKey, List<ModValue>>();
        foreach (var activeManifest in await PbDbContext.ModFileManifestHashes
            .SelectMany(mfmh => mfmh.ManifestsByCalculation!)
            .Where(mfm => mfm.ModFileHash!.ModFiles!.Any(mf => mf.Path != null && mf.AbsenceNoticed == null))
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
            .ToListAsync()
            .ConfigureAwait(false))
        {
            var key = new ModKey(activeManifest.Name, activeManifest.Creators?.Select(creator => creator.Name).Order().Humanize(), activeManifest.Url);
            if (!mods.TryGetValue(key, out var values))
            {
                values = [];
                mods.Add(key, values);
            }
            values.Add(new
            (
                activeManifest.ToModel(),
                activeManifest.ModFileHash!.ModFiles!.Where(mf => mf.Path is not null).Select(mf => new FileInfo(Path.Combine(userDataFolderPath, "Mods", mf.Path!))).ToList(),
                (activeManifest.RequiredMods ?? Enumerable.Empty<RequiredMod>()).Select(rm => new ModKey(rm.Name, rm.Creators?.Select(c => c.Name).Order().Humanize(), rm.Url)).Distinct().Except([key]).ToList(),
                (activeManifest.CalculatedModFileManifestHash?.Dependents ?? Enumerable.Empty<RequiredMod>()).Select(d => d.ModFileManifest).Where(mfm => mfm is not null).Cast<ModFileManifest>().Select(mfm => new ModKey(mfm.Name, mfm.Creators?.Select(c => c.Name).Order().Humanize(), mfm.Url)).Distinct().Except([key]).ToList()
            ));
        }
        var readOnlyMods = mods.ToImmutableDictionary(kv => kv.Key, kv => (IReadOnlyList<(ModFileManifestModel manifest, IReadOnlyList<FileInfo> files, IReadOnlyList<ModKey> dependencies, IReadOnlyList<ModKey> dependents)>)kv.Value.Select(manifestAndFiles => (manifestAndFiles.Manifest, (IReadOnlyList<FileInfo>)[.. manifestAndFiles.Files], (IReadOnlyList<ModKey>)[.. manifestAndFiles.Dependencies], (IReadOnlyList<ModKey>)[.. manifestAndFiles.Dependents])).ToImmutableArray());
        StaticDispatcher.Dispatch(() =>
        {
            this.mods = readOnlyMods;
            StateHasChanged();
        });
    }
}
