namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public class ModsDirectoryCataloger :
    IModsDirectoryCataloger
{
    const int estimateBackwardSample = 64;

    public static async ValueTask<(bool success, ModsDirectoryFileType fileType)> CatalogResourcesAndManifestsAsync(Microsoft.Extensions.Logging.ILogger logger, PbDbContext pbDbContext, FileInfo fileInfo, ModFileHash modFileHash, ModsDirectoryFileType fileType)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(fileInfo);
        ArgumentNullException.ThrowIfNull(modFileHash);
        if (!modFileHash.ResourcesAndManifestsCataloged || !modFileHash.StringTablesCataloged || fileType is ModsDirectoryFileType.Package && (modFileHash.DataBasePackedFileMajorVersion is null || modFileHash.DataBasePackedFileMinorVersion is null))
        {
            IDisposable? modFileAccessor = null;
            var ioExceptionGraceAttempts = 10;
            while (true)
            {
                try
                {
                    if (fileType is ModsDirectoryFileType.Package)
                        modFileAccessor = await DataBasePackedFile.FromPathAsync(fileInfo.FullName, forReadOnly: true).ConfigureAwait(false);
                    else if (fileType is ModsDirectoryFileType.ScriptArchive)
#pragma warning disable CA2000 // Dispose objects before losing scope
                        modFileAccessor = new ZipFile(fileInfo.OpenRead(), false);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    break;
                }
                catch (IOException ioEx) when (--ioExceptionGraceAttempts is >= 0)
                {
                    logger.LogWarning(ioEx, "unhandled exception when trying to open {Path} for read; {GraceAttempts} grace attempt(s) remain", fileInfo.FullName, ioExceptionGraceAttempts);
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch
                {
                    fileType = fileType is ModsDirectoryFileType.Package
                        ? ModsDirectoryFileType.CorruptPackage
                        : fileType is ModsDirectoryFileType.ScriptArchive
                        ? ModsDirectoryFileType.CorruptScriptArchive
                        : fileType;
                    modFileHash.IsCorrupt = true;
                    break;
                }
            }
            try
            {
                if (modFileAccessor is DataBasePackedFile dbpf)
                {
                    if (!modFileHash.ResourcesAndManifestsCataloged)
                    {
                        var keys = await dbpf.GetKeysAsync().ConfigureAwait(false);
                        if (keys.Contains(GlobalModsManifestModel.ResourceKey))
                        {
                            // BAD PLAYER
                            dbpf.Dispose();
                            fileInfo.Delete();
                            // what we just did was noticed by SSO, a second sweep specifically of this file will be enqueued shortly
                            return (false, fileType);
                        }
                        modFileHash.Resources.Clear();
                        foreach (var modFileResource in keys.Select(key => new ModFileResource(modFileHash) { Key = key, ModFileHash = modFileHash }))
                            if (!modFileHash.Resources.Any(resource => resource.KeyType == modFileResource.KeyType && resource.KeyGroup == modFileResource.KeyGroup && resource.KeyFullInstance == modFileResource.KeyFullInstance))
                                modFileHash.Resources.Add(modFileResource);
                        foreach (var (manifestKey, manifest) in await ModFileManifestModel.GetModFileManifestsAsync(dbpf).ConfigureAwait(false))
                        {
                            var dbManifest = await TransformModFileManifestModelAsync(pbDbContext, modFileHash, await ModFileManifestModel.GetModFileHashAsync(dbpf, manifest.HashResourceKeys).ConfigureAwait(false), manifest, manifestKey).ConfigureAwait(false);
                            dbManifest.Key = manifestKey;
                            modFileHash.ModFileManifests.Add(dbManifest);
                        }
                    }
                    if (!modFileHash.StringTablesCataloged)
                        foreach (var modFileResource in modFileHash.Resources.Where(resource => resource.Key.Type is ResourceType.StringTable))
                        {
                            try
                            {
                                var stringTable = await dbpf.GetStringTableAsync(modFileResource.Key).ConfigureAwait(false);
                                foreach (var modFileStringTableEntry in stringTable.KeyHashes.Select(key => new ModFileStringTableEntry(modFileResource) { Key = key }))
                                    if (!modFileResource.StringTableEntries.Any(entry => entry.SignedKey == modFileStringTableEntry.SignedKey))
                                        modFileResource.StringTableEntries.Add(modFileStringTableEntry);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "encountered unhandled exception while parsing STBL {ResourceKey}", modFileResource.Key);
                            }
                        }
                    modFileHash.DataBasePackedFileMajorVersion = dbpf.FileVersion.Major;
                    modFileHash.DataBasePackedFileMinorVersion = dbpf.FileVersion.Minor;
                }
                else if (modFileAccessor is ZipFile zipFile && !modFileHash.ResourcesAndManifestsCataloged)
                {
                    var entries = zipFile.Cast<ZipEntry>();
                    if (entries.Any(e => e.Name == "plumbbuddy_proxy/__init__.pyc"))
                    {
                        // BAD PLAYER
                        ((IDisposable)zipFile).Dispose();
                        fileInfo.Delete();
                        // what we just did was noticed by SSO, a second sweep specifically of this file will be enqueued shortly
                        return (false, fileType);
                    }
                    foreach (var entry in entries)
                        modFileHash.ScriptModArchiveEntries.Add(new(modFileHash)
                        {
                            Comment = entry.Comment,
                            CompressedLength = entry.CompressedSize,
                            Crc32 = unchecked((uint)entry.Crc),
                            ExternalAttributes = entry.ExternalFileAttributes,
                            FullName = entry.Name,
                            IsEncrypted = entry.AESKeySize is not 0,
                            LastWriteTime = entry.DateTime,
                            Length = entry.Size,
                            ModFileHash = modFileHash
                        });
                    if (await ModFileManifestModel.GetModFileManifestAsync(zipFile).ConfigureAwait(false) is { } manifest)
                    {
                        var dbManifest = await TransformModFileManifestModelAsync(pbDbContext, modFileHash, ModFileManifestModel.GetModFileHash(zipFile, manifest.ExcludedEntries), manifest, null).ConfigureAwait(false);
                        modFileHash.ModFileManifests.Add(dbManifest);
                    }
                }
            }
            finally
            {
                modFileAccessor?.Dispose();
            }
            modFileHash.ResourcesAndManifestsCataloged = fileType is not (ModsDirectoryFileType.CorruptPackage or ModsDirectoryFileType.CorruptScriptArchive);
            modFileHash.StringTablesCataloged = fileType is not (ModsDirectoryFileType.CorruptPackage or ModsDirectoryFileType.CorruptScriptArchive);
        }

        return (true, fileType);
    }

    static byte[] GetByteArray(ImmutableArray<byte> immutableByteArray) =>
        GetByteArrays([immutableByteArray]).Single();

    static IEnumerable<byte[]> GetByteArrays(IEnumerable<ImmutableArray<byte>> immutableByteArrays)
    {
        foreach (var immutableByteArray in immutableByteArrays)
        {
            var immutableByteArrayOnStack = immutableByteArray;
            yield return Unsafe.As<ImmutableArray<byte>, byte[]>(ref immutableByteArrayOnStack);
        }
    }

    public static async ValueTask<(ModFileHash modFileHash, ModsDirectoryFileType fileType)> GetModFileHashAsync(PbDbContext pbDbContext, ModsDirectoryFileType fileType, ImmutableArray<byte> hash)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        var hashArray = Unsafe.As<ImmutableArray<byte>, byte[]>(ref hash);
        await pbDbContext.Database.ExecuteSqlRawAsync("INSERT INTO ModFileHashes (Sha256, ResourcesAndManifestsCataloged, IsCorrupt) VALUES ({0}, 0, 0) ON CONFLICT DO NOTHING", hashArray).ConfigureAwait(false);
        var modFileHash = await pbDbContext.ModFileHashes.Include(mfh => mfh.ModFiles).Include(mfh => mfh.Resources).FirstAsync(mfh => mfh.Sha256 == hashArray).ConfigureAwait(false);
        if (modFileHash.IsCorrupt)
        {
            if (fileType is ModsDirectoryFileType.Package)
                fileType = ModsDirectoryFileType.CorruptPackage;
            else if (fileType is ModsDirectoryFileType.ScriptArchive)
                fileType = ModsDirectoryFileType.CorruptScriptArchive;
        }
        return (modFileHash, fileType);
    }

    static ModsDirectoryFileType GetFileType(FileInfo fileInfo)
    {
        return fileInfo.Extension.ToUpperInvariant() switch
        {
            ".7Z" => ModsDirectoryFileType.SevenZipArchive,
            ".HTM" or ".HTML" => ModsDirectoryFileType.HtmlFile,
            ".PACKAGE" => ModsDirectoryFileType.Package,
            ".RAR" => ModsDirectoryFileType.RarArchive,
            ".TS4SCRIPT" => ModsDirectoryFileType.ScriptArchive,
            ".TXT" or ".LOG" => ModsDirectoryFileType.TextFile,
            ".ZIP" => ModsDirectoryFileType.ZipArchive,
            _ => ModsDirectoryFileType.Ignored
        };
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    public static async Task<ModFileManifest> TransformModFileManifestModelAsync(PbDbContext pbDbContext, ModFileHash modFileHash, ImmutableArray<byte> calculatedModFileManifestHash, ModFileManifestModel modFileManifestModel, ResourceKey? key)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(modFileHash);
        ArgumentNullException.ThrowIfNull(modFileManifestModel);
        var modFileManifest = new ModFileManifest(modFileHash, await TransformNormalizedEntity
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModFileManifestHashes,
            nameof(ModFileManifestHash.Sha256),
            hash => mfmh => mfmh.Sha256 == hash,
            GetByteArray(modFileManifestModel.Hash)
        ).ConfigureAwait(false), await TransformNormalizedEntity
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModFileManifestHashes,
            nameof(ModFileManifestHash.Sha256),
            hash => mfmh => mfmh.Sha256 == hash,
            GetByteArray(calculatedModFileManifestHash)
        ).ConfigureAwait(false))
        {
            ContactEmail = modFileManifestModel.ContactEmail,
            ContactUrl = modFileManifestModel.ContactUrl,
            Description = modFileManifestModel.Description,
            ElectronicArtsPromoCode =
                !string.IsNullOrWhiteSpace(modFileManifestModel.ElectronicArtsPromoCode)
                ? await TransformNormalizedEntity
                (
                    pbDbContext,
                    pbDbContext => pbDbContext.ElectronicArtsPromoCodes,
                    nameof(ElectronicArtsPromoCode.Code),
                    code => eapc => eapc.Code == code,
                    modFileManifestModel.ElectronicArtsPromoCode
                ).ConfigureAwait(false)
                : null,
            Key = key,
            MessageToTranslators = modFileManifestModel.MessageToTranslators,
            Name = modFileManifestModel.Name ?? string.Empty,
            TranslationSubmissionUrl = modFileManifestModel.TranslationSubmissionUrl,
            TuningFullInstance = modFileManifestModel.TuningFullInstance is not 0
                ? unchecked((long)modFileManifestModel.TuningFullInstance)
                : null,
            TuningName = modFileManifestModel.TuningName,
            Url = modFileManifestModel.Url,
            Version = modFileManifestModel.Version
        };
        await foreach (var creator in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModCreators,
            nameof(ModCreator.Name),
            creator => mc => mc.Name == creator,
            modFileManifestModel.Creators
        ).ConfigureAwait(false))
            modFileManifest.Creators.Add(creator);
        foreach (var excludedEntry in modFileManifestModel.ExcludedEntries)
            if (excludedEntry is { } nonNullExcludedEntry)
                modFileManifest.ExcludedEntries.Add(new ModFileExcludedEntry(modFileManifest) { Name = nonNullExcludedEntry });
        await foreach (var exclusivity in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModExclusivities,
            nameof(ModExclusivity.Name),
            exclusivity => me => me.Name == exclusivity,
            modFileManifestModel.Exclusivities
        ).ConfigureAwait(false))
            modFileManifest.Exclusivities.Add(exclusivity);
        await foreach (var feature in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModFeatures,
            nameof(ModFeature.Name),
            feature => mf => mf.Name == feature,
            modFileManifestModel.Features
        ).ConfigureAwait(false))
            modFileManifest.Features.Add(feature);
        foreach (var hashResourceKey in modFileManifestModel.HashResourceKeys)
            modFileManifest.HashResourceKeys.Add(new ModFileManifestResourceKey(modFileManifest) { Key = hashResourceKey });
        await foreach (var incompatiblePack in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.PackCodes,
            nameof(PackCode.Code),
            code => pc => pc.Code == code,
            modFileManifestModel.IncompatiblePacks
        ).ConfigureAwait(false))
            modFileManifest.IncompatiblePacks.Add(incompatiblePack);
        foreach (var repurposedLanguage in modFileManifestModel.RepurposedLanguages.Where(rl => rl.ActualLocale is not null && rl.GameLocale is not null))
            modFileManifest.RepurposedLanguages.Add(new ModFileManifestRepurposedLanguage(modFileManifest)
            {
                ActualLocale = repurposedLanguage.ActualLocale,
                GameLocale = repurposedLanguage.GameLocale
            });
        foreach (var recommendedPack in modFileManifestModel.RecommendedPacks.Where(rp => !string.IsNullOrWhiteSpace(rp.PackCode)))
            modFileManifest.RecommendedPacks.Add
            (
                new Data.RecommendedPack
                (
                    modFileManifest,
                    await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.PackCodes,
                        nameof(PackCode.Code),
                        packCode => pc => pc.Code == packCode,
                        recommendedPack.PackCode
                    ).ConfigureAwait(false)
                )
                {
                    Reason = recommendedPack.Reason
                }
            );
        foreach (var requiredMod in modFileManifestModel.RequiredMods)
        {
            var modFileManifestRequiredMod = new RequiredMod(modFileManifest)
            {
                IgnoreIfHashAvailable =
                    !requiredMod.IgnoreIfHashAvailable.IsDefaultOrEmpty
                    ? await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.ModFileManifestHashes,
                        nameof(ModFileManifestHash.Sha256),
                        hash => mfmh => mfmh.Sha256 == hash,
                        GetByteArray(requiredMod.IgnoreIfHashAvailable)
                    ).ConfigureAwait(false)
                    : null,
                IgnoreIfHashUnavailable =
                    !requiredMod.IgnoreIfHashUnavailable.IsDefaultOrEmpty
                    ? await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.ModFileManifestHashes,
                        nameof(ModFileManifestHash.Sha256),
                        hash => mfmh => mfmh.Sha256 == hash,
                        GetByteArray(requiredMod.IgnoreIfHashUnavailable)
                    ).ConfigureAwait(false)
                    : null,
                IgnoreIfPackAvailable =
                    !string.IsNullOrWhiteSpace(requiredMod.IgnoreIfPackAvailable)
                    ? await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.PackCodes,
                        nameof(PackCode.Code),
                        packCode => pc => pc.Code == packCode,
                        requiredMod.IgnoreIfPackAvailable
                    ).ConfigureAwait(false)
                    : null,
                IgnoreIfPackUnavailable =
                    !string.IsNullOrWhiteSpace(requiredMod.IgnoreIfPackUnavailable)
                    ? await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.PackCodes,
                        nameof(PackCode.Code),
                        packCode => pc => pc.Code == packCode,
                        requiredMod.IgnoreIfPackUnavailable
                    ).ConfigureAwait(false)
                    : null,
                Name = requiredMod.Name ?? string.Empty,
                RequirementIdentifier = requiredMod.RequirementIdentifier is { } identifier
                    ? await TransformNormalizedEntity
                    (
                        pbDbContext,
                        pbDbContext => pbDbContext.RequirementIdentifiers,
                        nameof(RequirementIdentifier.Identifier),
                        requirementIdentifier => ri => ri.Identifier == requirementIdentifier,
                        requiredMod.RequirementIdentifier
                    ).ConfigureAwait(false)
                    : null,
                Url = requiredMod.Url,
                Version = requiredMod.Version
            };
            await foreach (var creator in TransformNormalizedEntitySequence
            (
                pbDbContext,
                pbDbContext => pbDbContext.ModCreators,
                nameof(ModCreator.Name),
                creator => mc => mc.Name == creator,
                requiredMod.Creators
            ).ConfigureAwait(false))
                modFileManifestRequiredMod.Creators.Add(creator);
            await foreach (var hash in TransformNormalizedEntitySequence
            (
                pbDbContext,
                pbDbContext => pbDbContext.ModFileManifestHashes,
                nameof(ModFileManifestHash.Sha256),
                hash => mfmh => mfmh.Sha256 == hash,
                GetByteArrays(requiredMod.Hashes)
            ).ConfigureAwait(false))
                modFileManifestRequiredMod.Hashes.Add(hash);
            await foreach (var requiredFeature in TransformNormalizedEntitySequence
            (
                pbDbContext,
                pbDbContext => pbDbContext.ModFeatures,
                nameof(ModFeature.Name),
                requiredFeature => mf => mf.Name == requiredFeature,
                requiredMod.RequiredFeatures
            ).ConfigureAwait(false))
                modFileManifestRequiredMod.RequiredFeatures.Add(requiredFeature);
            modFileManifest.RequiredMods.Add(modFileManifestRequiredMod);
        }
        await foreach (var requiredPack in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.PackCodes,
            nameof(PackCode.Code),
            code => pc => pc.Code == code,
            modFileManifestModel.RequiredPacks
        ).ConfigureAwait(false))
            modFileManifest.RequiredPacks.Add(requiredPack);
        await foreach (var subsumedHash in TransformNormalizedEntitySequence
        (
            pbDbContext,
            pbDbContext => pbDbContext.ModFileManifestHashes,
            nameof(ModFileManifestHash.Sha256),
            hash => mfmh => mfmh.Sha256 == hash,
            GetByteArrays(modFileManifestModel.SubsumedHashes)
        ).ConfigureAwait(false))
            modFileManifest.SubsumedHashes.Add(subsumedHash);
        foreach (var translator in modFileManifestModel.Translators)
            modFileManifest.Translators.Add(new ModFileManifestTranslator(modFileManifest)
            {
                Language = translator.Language,
                Name = translator.Name
            });
        return modFileManifest;
    }

    static ValueTask<TNormalizedEntity> TransformNormalizedEntity<TNormalizedValue, TNormalizedEntity>(PbDbContext pbDbContext, Func<PbDbContext, DbSet<TNormalizedEntity>> setSelector, string uniqueColumnName, Func<TNormalizedValue, Expression<Func<TNormalizedEntity, bool>>> selectionPredicateFactory, TNormalizedValue value)
        where TNormalizedEntity : class =>
        TransformNormalizedEntitySequence(pbDbContext, setSelector, uniqueColumnName, selectionPredicateFactory, [value]).FirstAsync();

    [SuppressMessage("Security", "EF1002: Risk of vulnerability to SQL injection.", Justification = "No, CA, it's just the name of the table. 🤦‍♂️")]
    static async IAsyncEnumerable<TNormalizedEntity> TransformNormalizedEntitySequence<TNormalizedValue, TNormalizedEntity>(PbDbContext pbDbContext, Func<PbDbContext, DbSet<TNormalizedEntity>> setSelector, string uniqueColumnName, Func<TNormalizedValue, Expression<Func<TNormalizedEntity, bool>>> selectionPredicateFactory, IEnumerable<TNormalizedValue> values)
        where TNormalizedEntity : class
    {
        var set = setSelector(pbDbContext);
        var entityType = pbDbContext.Model.FindEntityType(typeof(TNormalizedEntity))
            ?? throw new InvalidOperationException($"could not find entity type for {typeof(TNormalizedEntity)}");
        var tableName = entityType.GetTableMappings().Select(tableMapping => tableMapping.Table.Name).Distinct().Single();
        foreach (var value in values)
        {
            if (value is null)
                continue;
            await pbDbContext.Database.ExecuteSqlRawAsync($"INSERT INTO {tableName} ({uniqueColumnName}) VALUES ({{0}}) ON CONFLICT DO NOTHING", value).ConfigureAwait(false);
            yield return await set.FirstAsync(selectionPredicateFactory(value)).ConfigureAwait(false);
        }
    }

    static readonly PropertyChangedEventArgs estimatedStateTimeRemainingPropertyChangedEventArgs = new(nameof(EstimatedStateTimeRemaining));
    static readonly PropertyChangedEventArgs packageCountPropertyChangedEventArgs = new(nameof(PackageCount));
    static readonly PropertyChangedEventArgs progressMaxPropertyChangedEventArgs = new(nameof(ProgressMax));
    static readonly PropertyChangedEventArgs progressValuePropertyChangedEventArgs = new(nameof(ProgressValue));
    static readonly PropertyChangedEventArgs pythonByteCodeFileCountPropertyChangedEventArgs = new(nameof(PythonByteCodeFileCount));
    static readonly PropertyChangedEventArgs pythonScriptCountPropertyChangedEventArgs = new(nameof(PythonScriptCount));
    static readonly PropertyChangedEventArgs resourceCountPropertyChangedEventArgs = new(nameof(ResourceCount));
    static readonly PropertyChangedEventArgs scriptArchiveCountPropertyChangedEventArgs = new(nameof(ScriptArchiveCount));
    static readonly PropertyChangedEventArgs statePropertyChangedEventArgs = new(nameof(State));
    static readonly TimeSpan oneSecond = TimeSpan.FromSeconds(1);

    public ModsDirectoryCataloger(ILogger<IModsDirectoryCataloger> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, ISettings settings, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.settings = settings;
        this.superSnacks = superSnacks;
        awakeManualResetEvent = new(true);
        busyManualResetEvent = new(true);
        idleManualResetEvent = new(true);
        pathsProcessingQueue = new();
        uniqueIndexConstraintViolationStrikes = new();
        Task.Run(UpdateAggregatePropertiesAsync);
        Task.Run(ProcessPathsQueueAsync);
    }

    readonly AsyncManualResetEvent awakeManualResetEvent;
    readonly AsyncManualResetEvent busyManualResetEvent;
    TimeSpan? estimatedStateTimeRemaining;
    readonly AsyncManualResetEvent idleManualResetEvent;
    readonly ILogger<IModsDirectoryCataloger> logger;
    int packageCount;
    readonly AsyncProducerConsumerQueue<string> pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ISettings settings;
    int? progressMax;
    int progressValue;
    int pythonByteCodeFileCount;
    int pythonScriptCount;
    int resourceCount;
    int scriptArchiveCount;
    ModsDirectoryCatalogerState state;
    readonly ISuperSnacks superSnacks;
    readonly ConcurrentDictionary<string, ImmutableArray<DbUpdateException>> uniqueIndexConstraintViolationStrikes;

    public TimeSpan? EstimatedStateTimeRemaining
    {
        get => estimatedStateTimeRemaining;
        private set
        {
            if (estimatedStateTimeRemaining == value)
                return;
            estimatedStateTimeRemaining = value;
            OnPropertyChanged(estimatedStateTimeRemainingPropertyChangedEventArgs);
        }
    }

    public int PackageCount
    {
        get => packageCount;
        private set
        {
            if (packageCount == value)
                return;
            packageCount = value;
            OnPropertyChanged(packageCountPropertyChangedEventArgs);
        }
    }

    public int? ProgressMax
    {
        get => progressMax;
        private set
        {
            if (progressMax == value)
                return;
            progressMax = value;
            OnPropertyChanged(progressMaxPropertyChangedEventArgs);
        }
    }

    public int ProgressValue
    {
        get => progressValue;
        private set
        {
            if (progressValue == value)
                return;
            progressValue = value;
            OnPropertyChanged(progressValuePropertyChangedEventArgs);
        }
    }

    public int PythonByteCodeFileCount
    {
        get => pythonByteCodeFileCount;
        private set
        {
            if (pythonByteCodeFileCount == value)
                return;
            pythonByteCodeFileCount = value;
            OnPropertyChanged(pythonByteCodeFileCountPropertyChangedEventArgs);
        }
    }

    public int PythonScriptCount
    {
        get => pythonScriptCount;
        private set
        {
            if (pythonScriptCount == value)
                return;
            pythonScriptCount = value;
            OnPropertyChanged(pythonScriptCountPropertyChangedEventArgs);
        }
    }

    public int ResourceCount
    {
        get => resourceCount;
        private set
        {
            if (resourceCount == value)
                return;
            resourceCount = value;
            OnPropertyChanged(resourceCountPropertyChangedEventArgs);
        }
    }

    public int ScriptArchiveCount
    {
        get => scriptArchiveCount;
        private set
        {
            if (scriptArchiveCount == value)
                return;
            scriptArchiveCount = value;
            OnPropertyChanged(scriptArchiveCountPropertyChangedEventArgs);
        }
    }

    public ModsDirectoryCatalogerState State
    {
        get => state;
        private set
        {
            if (state == value)
                return;
            state = value;
            OnPropertyChanged(statePropertyChangedEventArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Catalog(string path) =>
        pathsProcessingQueue.Enqueue(path);

    public void GoToSleep() =>
        awakeManualResetEvent.Reset();

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task ProcessPathsQueueAsync()
    {
        while (await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            idleManualResetEvent.Reset();
            busyManualResetEvent.Set();
            State = ModsDirectoryCatalogerState.Sleeping;
            await awakeManualResetEvent.WaitAsync().ConfigureAwait(false);
            State = ModsDirectoryCatalogerState.Debouncing;
            var nomNom = new Queue<string>();
            nomNom.Enqueue(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
            while (true)
            {
                await Task.Delay(oneSecond).ConfigureAwait(false);
                State = ModsDirectoryCatalogerState.Sleeping;
                await awakeManualResetEvent.WaitAsync().ConfigureAwait(false);
                State = ModsDirectoryCatalogerState.Composing;
                await ManifestEditor.WaitForCompositionClearanceAsync().ConfigureAwait(false);
                State = ModsDirectoryCatalogerState.Debouncing;
                try
                {
                    if (!await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                        break;
                }
                catch (OperationCanceledException) // this was OutputAvailableAsync -- usually Mr. Cleary documents his throws 🙄
                {
                    // if we're here, it's because the processing queue is empty -- time to start eating
                    break;
                }
                try
                {
                    while (await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                    {
                        var path = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                        if (!nomNom.Contains(path))
                            nomNom.Enqueue(path);
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            try
            {
                State = ModsDirectoryCatalogerState.Cataloging;
                platformFunctions.ProgressState = AppProgressState.Indeterminate;
                var checkTopology = false;
                var completeRescan = false;
                while (nomNom.TryDequeue(out var path))
                {
                    using var pathPbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    ImmutableDictionary<string, (DateTimeOffset Creation, DateTimeOffset LastWrite, long Size)> fullyCatalogedFiles;
                    try
                    {
                        fullyCatalogedFiles = (await pathPbDbContext.ModFiles
                            .Where
                            (
                                mf =>
                                mf.ModFileHash.ResourcesAndManifestsCataloged
                                && mf.FoundAbsent == null
                                &&
                                (
                                    mf.FileType != ModsDirectoryFileType.Package
                                    || mf.ModFileHash.StringTablesCataloged
                                    && mf.ModFileHash.DataBasePackedFileMajorVersion != null
                                    && mf.ModFileHash.DataBasePackedFileMinorVersion != null
                                )
                            )
                            .Select(mf => new
                            {
                                Path = Path.Combine(settings.UserDataFolderPath, "Mods", mf.Path),
                                mf.Creation,
                                mf.LastWrite,
                                mf.Size
                            })
                            .ToListAsync()
                            .ConfigureAwait(false))
                            .ToImmutableDictionary(mf => mf.Path, mf => (Creation: mf.Creation.TrimSeconds(), LastWrite: mf.LastWrite.TrimSeconds(), mf.Size));
                    }
                    catch (ArgumentException)
                    {
                        var now = DateTimeOffset.UtcNow;
                        await pathPbDbContext.ModFiles
                            .Where(mf => mf.FoundAbsent == null)
                            .ExecuteUpdateAsync(mf => mf.SetProperty(mf => mf.FoundAbsent, now))
                            .ConfigureAwait(false);
                        Catalog(string.Empty);
                        completeRescan = true;
                        break;
                    }
                    var filesOfInterestPath = Path.Combine("Mods", path);
                    var modsDirectoryPath = Path.Combine(settings.UserDataFolderPath, "Mods");
                    var modsDirectoryInfo = new DirectoryInfo(modsDirectoryPath);
                    var fullPath = Path.Combine(modsDirectoryPath, path);
                    if (File.Exists(fullPath))
                    {
                        await ProcessDequeuedFileAsync(modsDirectoryInfo, new FileInfo(fullPath)).ConfigureAwait(false);
                        checkTopology = true;
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        var modsDirectoryFiles = new DirectoryInfo(fullPath)
                            .GetFiles("*.*", SearchOption.AllDirectories)
                            .ToLookup
                            (
                                mdm =>
                                !fullyCatalogedFiles.TryGetValue(mdm.FullName, out var fullyCatalogedFile)
                                || ((DateTimeOffset)mdm.CreationTimeUtc).TrimSeconds() != fullyCatalogedFile.Creation
                                || ((DateTimeOffset)mdm.LastWriteTimeUtc).TrimSeconds() != fullyCatalogedFile.LastWrite
                                || mdm.Length != fullyCatalogedFile.Size
                            );
                        var filesCataloged = 0;
                        var filesToCatalog = modsDirectoryFiles[true].Count();
                        ProgressValue = 0;
                        ProgressMax = filesToCatalog;
                        platformFunctions.ProgressState = AppProgressState.Normal;
                        platformFunctions.ProgressMaximum = progressMax!.Value;
                        var preservedModFilePaths = new ConcurrentBag<string>(modsDirectoryFiles[false].Select(fileInfo => fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]));
                        var preservedFileOfInterestPaths = new ConcurrentBag<string>();
                        using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2)))
                        {
                            var timeAtCataloged = new ConcurrentDictionary<int, DateTimeOffset>();
                            await Task.WhenAll(modsDirectoryFiles[true]
                                .Select(async fileInfo =>
                                {
                                    await semaphore.WaitAsync().ConfigureAwait(false);
                                    try
                                    {
                                        await ProcessDequeuedFileAsync(modsDirectoryInfo, fileInfo).ConfigureAwait(false);
                                        var newFilesCataloged = Interlocked.Increment(ref filesCataloged);
                                        ProgressValue = newFilesCataloged;
                                        platformFunctions.ProgressValue = progressValue;
                                        if (newFilesCataloged % estimateBackwardSample is 0)
                                        {
                                            timeAtCataloged.TryAdd(newFilesCataloged, DateTimeOffset.Now);
                                            if (timeAtCataloged.TryGetValue(newFilesCataloged - estimateBackwardSample, out var timeSomeFilesAgo))
                                                EstimatedStateTimeRemaining = new TimeSpan((DateTimeOffset.Now - timeSomeFilesAgo).Ticks / estimateBackwardSample * (filesToCatalog - newFilesCataloged) / 10000000 * 10000000 + 10000000);
                                        }
                                        var fileType = GetFileType(fileInfo);
                                        if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.ScriptArchive)
                                            preservedModFilePaths.Add(fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]);
                                        else if (fileType is not ModsDirectoryFileType.Ignored)
                                            preservedFileOfInterestPaths.Add(Path.Combine("Mods", fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]));
                                    }
                                    finally
                                    {
                                        semaphore.Release();
                                    }
                                })).ConfigureAwait(false);
                        }
                        var connection = (SqliteConnection)pathPbDbContext.Database.GetDbConnection();
                        if (connection.State is not ConnectionState.Open)
                            await connection.OpenAsync().ConfigureAwait(false);
                        var command = connection.CreateCommand();
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            DROP TABLE IF EXISTS ModFilePreservedPaths
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command.CommandText =
                            """
                            CREATE TEMP TABLE IF NOT EXISTS ModFilePreservedPaths (
                                Path TEXT PRIMARY KEY
                            ) WITHOUT ROWID;
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        foreach (var preservedPathsChunk in preservedModFilePaths.Chunk(800))
                        {
                            command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                            command.CommandText =
                                $"""
                                INSERT INTO ModFilePreservedPaths (Path) VALUES {string.Join(", ", preservedPathsChunk.Select((path, i) =>
                                {
                                    var paramName = $"@p{i}";
                                    command.Parameters.AddWithValue(paramName, path);
                                    return $"({paramName})";
                                }))};
                                """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                        command = connection.CreateCommand();
                        var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        command.CommandText =
                            """
                            UPDATE
                                ModFiles
                            SET
                                FoundAbsent = @nowEpoch
                            WHERE
                                FoundAbsent IS NULL
                                AND Path LIKE @pathPattern ESCAPE '|'
                                AND Path NOT IN
                                (
                                    SELECT
                                        Path
                                    FROM
                                        ModFilePreservedPaths
                                )
                                AND FileType IN (@packageFileType, @corruptPackageFileType)
                            """;
                        var pathPattern = Path.Combine($"{path.Replace("|", "||", StringComparison.Ordinal).Replace("_", "|_", StringComparison.Ordinal).Replace("%", "|%", StringComparison.Ordinal)}", "%");
                        command.Parameters.AddWithValue("@nowEpoch", nowEpoch);
                        command.Parameters.AddWithValue("@pathPattern", pathPattern);
                        command.Parameters.AddWithValue("@packageFileType", ModsDirectoryFileType.Package);
                        command.Parameters.AddWithValue("@corruptPackageFileType", ModsDirectoryFileType.CorruptPackage);
                        PackageCount -= await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            UPDATE
                                ModFiles
                            SET
                                FoundAbsent = @nowEpoch
                            WHERE
                                FoundAbsent IS NULL
                                AND Path LIKE @pathPattern ESCAPE '|'
                                AND Path NOT IN
                                (
                                    SELECT
                                        Path
                                    FROM
                                        ModFilePreservedPaths
                                )
                                AND FileType IN (@scriptArchiveFileType, @corruptScriptArchiveFileType)
                            """;
                        command.Parameters.AddWithValue("@nowEpoch", nowEpoch);
                        command.Parameters.AddWithValue("@pathPattern", pathPattern);
                        command.Parameters.AddWithValue("@scriptArchiveFileType", ModsDirectoryFileType.ScriptArchive);
                        command.Parameters.AddWithValue("@corruptScriptArchiveFileType", ModsDirectoryFileType.CorruptScriptArchive);
                        ScriptArchiveCount -= await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            DROP TABLE IF EXISTS ModFilePreservedPaths
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            DROP TABLE IF EXISTS FileOfInterestPreservedPaths
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command.CommandText =
                            """
                            CREATE TEMP TABLE IF NOT EXISTS FileOfInterestPreservedPaths (
                                Path TEXT PRIMARY KEY
                            ) WITHOUT ROWID;
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        foreach (var preservedPathsChunk in preservedFileOfInterestPaths.Chunk(800))
                        {
                            command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                            command.CommandText =
                                $"""
                                INSERT INTO FileOfInterestPreservedPaths (Path) VALUES {string.Join(", ", preservedPathsChunk.Select((path, i) =>
                                {
                                    var paramName = $"@p{i}";
                                    command.Parameters.AddWithValue(paramName, path);
                                    return $"({paramName})";
                                }))};
                                """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            DELETE FROM
                                FilesOfInterest
                            WHERE
                                Path LIKE @pathPattern ESCAPE '|'
                                AND Path NOT IN
                                (
                                    SELECT
                                        Path
                                    FROM
                                        FileOfInterestPreservedPaths
                                )
                            """;
                        var filesOfInterestPathPattern = Path.Combine($"{filesOfInterestPath.Replace("|", "||", StringComparison.Ordinal).Replace("_", "|_", StringComparison.Ordinal).Replace("%", "|%", StringComparison.Ordinal)}", "%");
                        command.Parameters.AddWithValue("@pathPattern", filesOfInterestPathPattern);
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        command = connection.CreateCommand();
                        command.CommandText =
                            """
                            DROP TABLE IF EXISTS FileOfInterestPreservedPaths
                            """;
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                        if (modsDirectoryFiles[true].Any(mdf => mdf.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase) && mdf.FullName[(modsDirectoryInfo.FullName.Length + 1)..] is not SmartSimObserver.IntegrationPackageName))
                            checkTopology = true;
                    }
                    else
                    {
                        var now = DateTimeOffset.UtcNow;
                        var pathPrefix = $"{path}{(!string.IsNullOrEmpty(path) && string.IsNullOrEmpty(Path.GetExtension(path)) ? Path.DirectorySeparatorChar : string.Empty)}";
                        var packagesRemoved = await pathPbDbContext.ModFiles
                            .Where(mf => mf.FoundAbsent == null && mf.Path.StartsWith(pathPrefix) && (mf.FileType == ModsDirectoryFileType.Package || mf.FileType == ModsDirectoryFileType.CorruptPackage))
                            .ExecuteUpdateAsync(mf => mf.SetProperty(mf => mf.FoundAbsent, now))
                            .ConfigureAwait(false);
                        PackageCount -= packagesRemoved;
                        if (packagesRemoved is > 0)
                            checkTopology = true;
                        ScriptArchiveCount -= await pathPbDbContext.ModFiles
                            .Where(mf => mf.FoundAbsent == null && mf.Path.StartsWith(pathPrefix) && (mf.FileType == ModsDirectoryFileType.ScriptArchive || mf.FileType == ModsDirectoryFileType.CorruptScriptArchive))
                            .ExecuteUpdateAsync(mf => mf.SetProperty(mf => mf.FoundAbsent, now))
                            .ConfigureAwait(false);
                        await pathPbDbContext.FilesOfInterest
                            .Where(foi => foi.Path.StartsWith(filesOfInterestPath))
                            .ExecuteDeleteAsync()
                            .ConfigureAwait(false);
                    }
                }
                if (completeRescan)
                    continue;
                EstimatedStateTimeRemaining = null;
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
                State = ModsDirectoryCatalogerState.AnalyzingTopology;
                ProgressMax = null;
                platformFunctions.ProgressState = AppProgressState.Indeterminate;
                // add mod file player record / mod file hash links that should exist (a mod file was updated)
                await pbDbContext.Database.ExecuteSqlRawAsync
                (
                    $"""
                    INSERT INTO
                        ModFileHashModFilePlayerRecord (ModFilePlayerRecordsId, ModFileHashesId)
                    SELECT DISTINCT
                        mfpr.Id,
                        mf.ModFileHashId
                    FROM
                        ModFilePlayerRecords mfpr
                        JOIN ModFilePlayerRecordPaths mfprp ON mfprp.ModFilePlayerRecordId = mfpr.Id
                        JOIN ModFiles mf ON mf.Path = mfprp.Path
                    WHERE
                        mf.FoundAbsent IS NULL
                    ON CONFLICT DO NOTHING
                    """
                ).ConfigureAwait(false);
                // add mod file player record paths that should exist (a mod file was moved)
                await pbDbContext.Database.ExecuteSqlRawAsync
                (
                    $"""
                    INSERT INTO
                        ModFilePlayerRecordPaths (ModFilePlayerRecordId, Path)
                    SELECT
                        mfpr.Id,
                        mf.Path
                    FROM
                        ModFilePlayerRecords mfpr
                        JOIN ModFileHashModFilePlayerRecord mfhmfpr ON mfhmfpr.ModFilePlayerRecordsId = mfpr.Id
                        JOIN ModFileHashes mfh ON mfh.Id = mfhmfpr.ModFileHashesId
                        JOIN ModFiles mf ON mf.ModFileHashId = mfh.Id
                    WHERE
                        mf.FoundAbsent IS NULL
                    ON CONFLICT DO NOTHING
                    """
                ).ConfigureAwait(false);
                // remove mod file player record paths for which there are no longer mod files (a mod file was deleted)
                await pbDbContext.Database.ExecuteSqlRawAsync
                (
                    $"""
                    DELETE FROM
                        ModFilePlayerRecordPaths
                    WHERE
                        Path NOT IN
                        (
                            SELECT
                                Path
                            FROM
                                ModFiles
                            WHERE
                                FoundAbsent IS NULL
                        )
                    """
                ).ConfigureAwait(false);
                var resourceWasRemovedOrReplaced = false;
                if (checkTopology)
                {
                    var latestTopologySnapshot = await pbDbContext.TopologySnapshots.OrderByDescending(ts => ts.Id).FirstOrDefaultAsync().ConfigureAwait(false);
                    var currentTopologySnapshot = new TopologySnapshot { Taken = DateTimeOffset.UtcNow };
                    await pbDbContext.TopologySnapshots.AddAsync(currentTopologySnapshot).ConfigureAwait(false);
                    await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                    var pathCollation = platformFunctions.FileSystemSQliteCollation;
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection
                    await pbDbContext.Database.ExecuteSqlRawAsync
                    (
                        $"""
                        INSERT INTO
                            ModFileResourceTopologySnapshot (TopologySnapshotsId, ResourcesId)
                        SELECT DISTINCT
                            {currentTopologySnapshot.Id},
                            sq.Id
                        FROM 
                            (
                                SELECT DISTINCT
                                    mfr.KeyType,
                                    mfr.KeyGroup,
                                    mfr.KeyFullInstance,
                                    FIRST_VALUE(mfr.Id) OVER (PARTITION BY mfr.KeyType, mfr.KeyGroup, mfr.KeyFullInstance ORDER BY mf.Path COLLATE {pathCollation}) Id
                                FROM
                                    ModFileResources mfr
                                    JOIN ModFileHashes mfh ON mfh.Id = mfr.ModFileHashId
                                    JOIN ModFiles mf ON mf.ModFileHashId = mfh.Id
                                WHERE
                                    mf.FoundAbsent IS NULL
                                    AND mf.FileType = 1
                            ) sq
                        """
                    ).ConfigureAwait(false);
                    if (latestTopologySnapshot is not null)
                    {
                        resourceWasRemovedOrReplaced =
                            (await pbDbContext.Database.SqlQueryRaw<int>
                            (
                                $"""
                                SELECT COUNT(*)
                                FROM (
                                    SELECT ResourcesId FROM ModFileResourceTopologySnapshot WHERE TopologySnapshotsId = {latestTopologySnapshot.Id}
                                    EXCEPT
                                    SELECT ResourcesId FROM ModFileResourceTopologySnapshot WHERE TopologySnapshotsId = {currentTopologySnapshot.Id}
                                )
                                """
                            ).ToListAsync().ConfigureAwait(false))[0] > 0;
                        await pbDbContext.TopologySnapshots.Where(ts => ts.Id != currentTopologySnapshot.Id).ExecuteDeleteAsync().ConfigureAwait(false);
                    }
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection
                }
                if (resourceWasRemovedOrReplaced && settings.CacheStatus is SmartSimCacheStatus.Normal)
                    settings.CacheStatus = SmartSimCacheStatus.Stale;
                platformFunctions.ProgressState = AppProgressState.None;
                platformFunctions.ProgressMaximum = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unhandled exception while processing the paths queue");
            }
            finally
            {
                State = ModsDirectoryCatalogerState.Idle;
                busyManualResetEvent.Reset();
                idleManualResetEvent.Set();
                platformFunctions.ProgressState = AppProgressState.None;
                platformFunctions.ProgressValue = 0;
                platformFunctions.ProgressMaximum = 0;
            }
        }
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task ProcessDequeuedFileAsync(DirectoryInfo modsDirectoryInfo, FileInfo fileInfo)
    {
        var fileType = GetFileType(fileInfo);
        if (fileType is ModsDirectoryFileType.Ignored)
            return;
        var path = fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..];
        var filesOfInterestPath = Path.Combine("Mods", path);
        var creation = ((DateTimeOffset)fileInfo.CreationTimeUtc).TrimSeconds();
        var lastWrite = ((DateTimeOffset)fileInfo.LastWriteTimeUtc).TrimSeconds();
        var size = fileInfo.Length;
        if (path is SmartSimObserver.IntegrationPackageName or SmartSimObserver.IntegrationScriptModName)
            return;
        try
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.ScriptArchive)
            {
                ModFile? modFile = null;
                ModFileHash? modFileHash = null;
                var modFileDetails = await pbDbContext.ModFiles
                    .Include(mf => mf.ModFileHash)
                    .Where
                    (
                        mf =>
                           mf.Path == path
                        && mf.Creation == creation
                        && mf.LastWrite == lastWrite
                        && mf.Size == size
                    )
                    .Select(mf => new
                    {
                        ModFile = mf,
                        ResourceCount = mf.ModFileHash.Resources.Count(),
                        ByteCodeEntries = mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".pyc")),
                        PythonEntries = mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".py"))
                    })
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                modFile = modFileDetails?.ModFile;
                if (modFile is not null)
                {
                    modFileHash = modFile.ModFileHash;
                    fileType = modFile.FileType;
                    var success = false;
                    (success, fileType) = await CatalogResourcesAndManifestsAsync(logger, pbDbContext, fileInfo, modFileHash, fileType).ConfigureAwait(false);
                    if (!success)
                        return;
                }
                if (modFile is null)
                {
                    var hash = await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false);
                    if (modFileHash is null)
                        (modFileHash, fileType) = await GetModFileHashAsync(pbDbContext, fileType, hash).ConfigureAwait(false);
                    var success = false;
                    (success, fileType) = await CatalogResourcesAndManifestsAsync(logger, pbDbContext, fileInfo, modFileHash, fileType).ConfigureAwait(false);
                    if (!success)
                        return;
                    modFile = modFileHash.ModFiles?.Where
                    (
                        mf =>
                        (mf.Path?.Equals(path, platformFunctions.FileSystemStringComparison) ?? false)
                        && mf.Creation == creation
                        && mf.LastWrite == lastWrite
                        && mf.Size == size
                    ).FirstOrDefault();
                }
                if (modFile is null)
                {
                    if (modFileHash is null)
                        (modFileHash, fileType) = await GetModFileHashAsync(pbDbContext, fileType, await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false));
                    modFile = new(modFileHash)
                    {
                        Path = path
                    };
                    modFileHash.ModFiles!.Add(modFile);
                    await pbDbContext.AddAsync(modFile).ConfigureAwait(false);
                }
                if (fileType is ModsDirectoryFileType.Package
                    && SmartSimObserver.IntegrationPackageLastSha256 is { IsDefaultOrEmpty: false } integrationPackageLastSha256
                    && modFileHash!.Sha256.SequenceEqual(integrationPackageLastSha256))
                {
                    try
                    {
                        // rogue integration package
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "failed to remove rogue integration package");
                    }
                    return;
                }
                if (fileType is ModsDirectoryFileType.ScriptArchive
                    && SmartSimObserver.IntegrationScriptModLastSha256 is { IsDefaultOrEmpty: false } integrationScriptModLastSha256
                    && modFileHash!.Sha256.SequenceEqual(integrationScriptModLastSha256))
                {
                    try
                    {
                        // rogue integration script archive
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "failed to remove rogue integration script archive");
                    }
                    return;
                }
                var replacedModFile = await pbDbContext.ModFiles
                    .Where(mf => mf.FoundAbsent == null && mf.Path == path)
                    .Select(mf => new
                    {
                        ResourceCount = mf.ModFileHash.Resources.Count(),
                        ByteCodeEntries = mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".pyc")),
                        PythonEntries = mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".py"))
                    })
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.CorruptPackage)
                {
                    if (replacedModFile is null)
                    {
                        Interlocked.Increment(ref packageCount);
                        OnPropertyChanged(packageCountPropertyChangedEventArgs);
                    }
                    else
                        Interlocked.Add(ref resourceCount, replacedModFile.ResourceCount * -1);
                    Interlocked.Add(ref resourceCount, modFileDetails?.ResourceCount ?? modFileHash!.Resources?.Count
                        ?? await pbDbContext.ModFileResources.CountAsync(mfr => mfr.ModFileHashId == modFileHash.Id).ConfigureAwait(false));
                    OnPropertyChanged(resourceCountPropertyChangedEventArgs);
                }
                else if (fileType is ModsDirectoryFileType.ScriptArchive or ModsDirectoryFileType.CorruptScriptArchive)
                {
                    if (replacedModFile is null)
                    {
                        Interlocked.Increment(ref scriptArchiveCount);
                        OnPropertyChanged(scriptArchiveCountPropertyChangedEventArgs);
                    }
                    else
                    {
                        Interlocked.Add(ref pythonByteCodeFileCount, replacedModFile.ByteCodeEntries * -1);
                        Interlocked.Add(ref pythonScriptCount, replacedModFile.PythonEntries * -1);
                    }
                    Interlocked.Add(ref pythonByteCodeFileCount, modFileDetails?.ByteCodeEntries ?? modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.FullName.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
                        ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.FullName.EndsWith(".pyc")));
                    OnPropertyChanged(pythonByteCodeFileCountPropertyChangedEventArgs);
                    Interlocked.Add(ref pythonScriptCount, modFileDetails?.PythonEntries ?? modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.FullName.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                        ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.FullName.EndsWith(".py")));
                    OnPropertyChanged(pythonScriptCountPropertyChangedEventArgs);
                }
                modFile.Creation = creation;
                modFile.LastWrite = lastWrite;
                modFile.Size = size;
                modFile.FileType = fileType;
                modFile.FoundAbsent = null;
                var now = DateTimeOffset.UtcNow;
                await pbDbContext.ModFiles
                    .Where(mf => mf.Path == path && mf.FoundAbsent == null && mf.Id != modFile.Id)
                    .ExecuteUpdateAsync(mf => mf.SetProperty(mf => mf.FoundAbsent, now))
                    .ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            else if (fileType is not ModsDirectoryFileType.Ignored && !await pbDbContext.FilesOfInterest.AnyAsync(foi => foi.Path == filesOfInterestPath).ConfigureAwait(false))
            {
                pbDbContext.FilesOfInterest.Add(new FileOfInterest
                {
                    FileType = fileType,
                    Path = filesOfInterestPath
                });
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // if this happens we really don't care because whatever enqueued paths are next will clear it up
            return;
        }
        catch (FileNotFoundException)
        {
            // if this happens we really don't care because whatever enqueued paths are next will clear it up
            return;
        }
        catch (DbUpdateException efCoreEx) when
        (
            efCoreEx.InnerException is SqliteException sqliteEx
            && sqliteEx.SqliteErrorCode is 19
            && uniqueIndexConstraintViolationStrikes.AddOrUpdate(path, _ => [efCoreEx], (_, array) => array.Add(efCoreEx)).Length is < 3
        )
        {
            // two threads probably tried to manipulate shared resources concurrently and it caused an issue, just reprocess this mod file
            Catalog(path);
            return;
        }
        catch (Exception ex)
        {
            var processEx = ex;
            if (uniqueIndexConstraintViolationStrikes.TryGetValue(path, out var strikes))
                processEx = new AggregateException("UNIQUE INDEX constraint violation strike threshold exceeded", strikes);
            logger.LogWarning(processEx, "unexpected exception encountered while processing {FilePath}", path);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModsDirectoryCataloger_Warning_CannotReadModFile, path, processEx.GetType().Name, processEx.Message)), Severity.Warning, options =>
            {
                options.Icon = MaterialDesignIcons.Normal.PackageVariantRemove;
                options.RequireInteraction = true;
            });
        }
    }

    async Task UpdateAggregatePropertiesAsync()
    {
        using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
    }

    async Task UpdateAggregatePropertiesAsync(PbDbContext pbDbContext)
    {
        PackageCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.FoundAbsent == null && (mf.FileType == ModsDirectoryFileType.Package || mf.FileType == ModsDirectoryFileType.CorruptPackage))
            .ConfigureAwait(false);
        PythonByteCodeFileCount = await pbDbContext.ModFiles
            .Where(mf => mf.FoundAbsent == null)
            .SumAsync(mf => mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".pyc")))
            .ConfigureAwait(false);
        PythonScriptCount = await pbDbContext.ModFiles
            .Where(mf => mf.FoundAbsent == null)
            .SumAsync(mf => mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.FullName.EndsWith(".py")))
            .ConfigureAwait(false);
        ResourceCount = await pbDbContext.ModFiles
            .Where(mf => mf.FoundAbsent == null)
            .SumAsync(mf => mf.ModFileHash.Resources.Count)
            .ConfigureAwait(false);
        ScriptArchiveCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.FoundAbsent == null && (mf.FileType == ModsDirectoryFileType.ScriptArchive || mf.FileType == ModsDirectoryFileType.CorruptScriptArchive))
            .ConfigureAwait(false);
    }

    public Task WaitForBusyAsync(CancellationToken cancellationToken = default) =>
        busyManualResetEvent.WaitAsync(cancellationToken);

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default) =>
        idleManualResetEvent.WaitAsync(cancellationToken);

    public void WakeUp() =>
        awakeManualResetEvent.Set();
}
