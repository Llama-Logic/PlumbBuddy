namespace PlumbBuddy.Services;

public partial class GameResourceCataloger :
    IGameResourceCataloger
{
    [GeneratedRegex(@"^[EFGS]P\d{2}$", RegexOptions.IgnoreCase)]
    private static partial Regex GetPackDirectoryNamePattern();

    public GameResourceCataloger(ILogger<GameResourceCataloger> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        scanDebouncer = new(ScanAsync, TimeSpan.FromSeconds(5));
    }

    bool isBusy;
    readonly ILogger<GameResourceCataloger> logger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly AsyncDebouncer scanDebouncer;
    readonly ISettings settings;

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (isBusy == value)
                return;
            isBusy = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    async Task ScanAsync()
    {
        try
        {
            var installationDirectory = new DirectoryInfo(settings.InstallationFolderPath);
            var stringsPackagesOnDisk = new Dictionary<string, (FileInfo file, bool isDelta)>(platformFunctions.FileSystemStringComparer);
            foreach (var clientStringsPackageFile in platformFunctions.GetClientDirectory(installationDirectory).GetFiles("Strings_*.package", SearchOption.TopDirectoryOnly))
                stringsPackagesOnDisk.Add(clientStringsPackageFile.FullName, (clientStringsPackageFile, false));
            foreach (var deltaStringsPackageFile in platformFunctions.GetDeltaDirectory(installationDirectory).GetFiles("Strings_*.package", SearchOption.AllDirectories))
                stringsPackagesOnDisk.Add(deltaStringsPackageFile.FullName, (deltaStringsPackageFile, true));
            foreach (var packDirectory in platformFunctions.GetPacksDirectory(installationDirectory).GetDirectories("*.*", SearchOption.TopDirectoryOnly))
                if (GetPackDirectoryNamePattern().IsMatch(packDirectory.Name))
                    foreach (var packStringsPackageFile in packDirectory.GetFiles("Strings_*.package", SearchOption.TopDirectoryOnly))
                        stringsPackagesOnDisk.Add(packStringsPackageFile.FullName, (packStringsPackageFile, false));
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await pbDbContext.GameStringsPackages.Where(gsp => !Enumerable.Contains(stringsPackagesOnDisk.Keys, gsp.Path)).ExecuteDeleteAsync().ConfigureAwait(false);
            var stringsPackagesCataloged = (await pbDbContext.GameStringsPackages.Include(gsp => gsp.Resources).ThenInclude(gspr => gspr.StringTableEntries).ToListAsync().ConfigureAwait(false)).ToImmutableDictionary(gsp => gsp.Path);
            foreach (var (file, isDelta) in stringsPackagesOnDisk.Values)
            {
                ImmutableArray<byte> sha256;
                if (stringsPackagesCataloged.TryGetValue(file.FullName, out var catalogedStringsPackage))
                {
                    if (catalogedStringsPackage.Creation == file.CreationTimeUtc
                        && catalogedStringsPackage.LastWrite == file.LastWriteTimeUtc
                        && catalogedStringsPackage.Size == file.Length)
                        continue;
                    sha256 = await ModFileManifestModel.GetFileSha256HashAsync(file.FullName).ConfigureAwait(false);
                    if (sha256.SequenceEqual(catalogedStringsPackage.Sha256))
                    {
                        catalogedStringsPackage.Creation = file.CreationTimeUtc;
                        catalogedStringsPackage.LastWrite = file.LastWriteTimeUtc;
                        catalogedStringsPackage.Size = file.Length;
                        continue;
                    }
                }
                else
                    sha256 = await ModFileManifestModel.GetFileSha256HashAsync(file.FullName).ConfigureAwait(false);
                if (catalogedStringsPackage is null)
                {
                    catalogedStringsPackage = new()
                    {
                        Creation = file.CreationTimeUtc,
                        IsDelta = isDelta,
                        LastWrite = file.LastWriteTimeUtc,
                        Path = file.FullName,
                        Sha256 = Unsafe.As<ImmutableArray<byte>, byte[]>(ref sha256),
                        Size = file.Length
                    };
                    await pbDbContext.GameStringsPackages.AddAsync(catalogedStringsPackage).ConfigureAwait(false);
                }
                using var dbpf = await DataBasePackedFile.FromPathAsync(file.FullName).ConfigureAwait(false);
                var resourceKeys = (await dbpf.GetKeysAsync().ConfigureAwait(false)).Where(key => key.Type is ResourceType.StringTable).ToImmutableHashSet();
                foreach (var noLongerExistingResource in catalogedStringsPackage.Resources.Where(resource => !resourceKeys.Contains(resource.Key)).ToImmutableArray())
                    catalogedStringsPackage.Resources.Remove(noLongerExistingResource);
                foreach (var resourceKey in resourceKeys)
                {
                    var resource = catalogedStringsPackage.Resources.FirstOrDefault(resource => resource.Key == resourceKey);
                    if (resource is null)
                    {
                        resource = new(catalogedStringsPackage)
                        {
                            Key = resourceKey,
                            StringTableLocalePrefix = (LocaleFullInstancePrefix)((resourceKey.FullInstance & 0xFF00000000000000) >> 56)
                        };
                        catalogedStringsPackage.Resources.Add(resource);
                    }
                    var stringTable = await dbpf.GetStringTableAsync(resourceKey).ConfigureAwait(false);
                    var keys = stringTable.KeyHashes.ToImmutableHashSet();
                    foreach (var noLongerExistingEntry in resource.StringTableEntries.Where(stringTableEntry => !keys.Contains(stringTableEntry.Key)).ToImmutableArray())
                        resource.StringTableEntries.Remove(noLongerExistingEntry);
                    var catalogedKeys = resource.StringTableEntries.Select(stringTableEntry => stringTableEntry.Key).ToImmutableHashSet();
                    foreach (var key in keys.Except(catalogedKeys))
                    {
                        var stringTableEntry = new GameStringTableEntry(resource) { Key = key };
                        resource.StringTableEntries.Add(stringTableEntry);
                    }
                }
            }
            await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "unhandled exception");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ScanSoon()
    {
        if (scanDebouncer.Execute())
            IsBusy = true;
    }
}
