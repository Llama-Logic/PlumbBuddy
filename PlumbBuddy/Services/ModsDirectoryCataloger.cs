using PlumbBuddy.Data;
using System.IO;

namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public class ModsDirectoryCataloger :
    IModsDirectoryCataloger
{
    const int estimateBackwardSample = 64;

    public static async ValueTask<(bool success, ModsDirectoryFileType fileType)> CatalogResourcesAndManifestsAsync(PbDbContext pbDbContext, FileInfo fileInfo, ModFileHash modFileHash, ModsDirectoryFileType fileType)
    {
        ArgumentNullException.ThrowIfNull(pbDbContext);
        ArgumentNullException.ThrowIfNull(fileInfo);
        ArgumentNullException.ThrowIfNull(modFileHash);
        if (!modFileHash.ResourcesAndManifestsCataloged)
        {
            if (fileType is ModsDirectoryFileType.Package)
            {
                DataBasePackedFile? dbpf = null;
                var ioExceptionGraceAttempts = 10;
                while (true)
                {
                    try
                    {
                        dbpf = await DataBasePackedFile.FromPathAsync(fileInfo.FullName, forReadOnly: true).ConfigureAwait(false);
                        break;
                    }
                    catch (IOException) when (--ioExceptionGraceAttempts is >= 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch
                    {
                        fileType = ModsDirectoryFileType.CorruptPackage;
                        modFileHash.IsCorrupt = true;
                        break;
                    }
                }
                try
                {
                    if (dbpf is not null)
                    {
                        var keys = await dbpf.GetKeysAsync().ConfigureAwait(false);
                        if (keys.Contains(GlobalModsManifestModel.ResourceKey))
                        {
                            // BAD PLAYER (or frick'n hacker)
                            dbpf.Dispose();
                            dbpf = await DataBasePackedFile.FromPathAsync(fileInfo.FullName, forReadOnly: false).ConfigureAwait(false);
                            dbpf.Delete(GlobalModsManifestModel.ResourceKey);
                            await dbpf.SaveAsync().ConfigureAwait(false);
                            // what we just did was noticed by SSO, a second sweep specifically of this file will be enqueued shortly
                            return (false, fileType);
                        }
                        modFileHash.Resources.Clear();
                        foreach (var key in keys.Select(key => new ModFileResource(modFileHash) { Key = key, ModFileHash = modFileHash }))
                            modFileHash.Resources.Add(key);
                        foreach (var (manifestKey, manifest) in await ModFileManifestModel.GetModFileManifestsAsync(dbpf).ConfigureAwait(false))
                        {
                            var dbManifest = await TransformModFileManifestModelAsync(pbDbContext, modFileHash, await ModFileManifestModel.GetModFileHashAsync(dbpf, manifest.HashResourceKeys).ConfigureAwait(false), manifest, manifestKey).ConfigureAwait(false);
                            dbManifest.Key = manifestKey;
                            modFileHash.ModFileManifests.Add(dbManifest);
                        }
                    }
                }
                finally
                {
                    dbpf?.Dispose();
                }
            }
            else if (fileType is ModsDirectoryFileType.ScriptArchive)
            {
                ZipArchive? zipArchive = null;
                var ioExceptionGraceAttempts = 10;
                while (true)
                {
                    try
                    {
                        zipArchive = ZipFile.OpenRead(fileInfo.FullName);
                        break;
                    }
                    catch (IOException) when (--ioExceptionGraceAttempts is >= 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                    catch
                    {
                        fileType = ModsDirectoryFileType.CorruptScriptArchive;
                        modFileHash.IsCorrupt = true;
                        break;
                    }
                }
                try
                {
                    if (zipArchive is not null)
                    {
                        foreach (var entry in zipArchive.Entries)
                            modFileHash.ScriptModArchiveEntries.Add(new(modFileHash)
                            {
                                Comment = entry.Comment,
                                CompressedLength = entry.CompressedLength,
                                Crc32 = entry.Crc32,
                                ExternalAttributes = entry.ExternalAttributes,
                                FullName = entry.FullName,
                                IsEncrypted = entry.IsEncrypted,
                                LastWriteTime = entry.LastWriteTime,
                                Length = entry.Length,
                                ModFileHash = modFileHash,
                                Name = entry.Name
                            });
                        if (await ModFileManifestModel.GetModFileManifestAsync(zipArchive).ConfigureAwait(false) is { } manifest)
                        {
                            var dbManifest = await TransformModFileManifestModelAsync(pbDbContext, modFileHash, ModFileManifestModel.GetModFileHash(zipArchive), manifest, null).ConfigureAwait(false);
                            modFileHash.ModFileManifests.Add(dbManifest);
                        }
                    }
                }
                finally
                {
                    zipArchive?.Dispose();
                }
            }
            modFileHash.ResourcesAndManifestsCataloged = fileType is not (ModsDirectoryFileType.CorruptPackage or ModsDirectoryFileType.CorruptScriptArchive);
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
        var modFileHash = await pbDbContext.ModFileHashes.Include(mfh => mfh.ModFiles).FirstAsync(mfh => mfh.Sha256 == hashArray).ConfigureAwait(false);
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

    public TimeSpan? EstimatedStateTimeRemaining
    {
        get => estimatedStateTimeRemaining;
        private set
        {
            if (estimatedStateTimeRemaining == value)
                return;
            estimatedStateTimeRemaining = value;
            PropertyChanged?.Invoke(this, estimatedStateTimeRemainingPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, packageCountPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, progressMaxPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, progressValuePropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, pythonByteCodeFileCountPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, pythonScriptCountPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, resourceCountPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, scriptArchiveCountPropertyChangedEventArgs);
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
            PropertyChanged?.Invoke(this, statePropertyChangedEventArgs);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Catalog(string path) =>
        pathsProcessingQueue.Enqueue(path);

    public void GoToSleep() =>
        awakeManualResetEvent.Reset();

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
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                while (nomNom.TryDequeue(out var path))
                {
                    var filesOfInterestPath = Path.Combine("Mods", path);
                    var modsDirectoryPath = Path.Combine(settings.UserDataFolderPath, "Mods");
                    var modsDirectoryInfo = new DirectoryInfo(modsDirectoryPath);
                    var fullPath = Path.Combine(modsDirectoryPath, path);
                    if (File.Exists(fullPath))
                        await ProcessDequeuedFileAsync(modsDirectoryInfo, new FileInfo(fullPath)).ConfigureAwait(false);
                    else if (Directory.Exists(fullPath))
                    {
                        var modsDirectoryFiles = new DirectoryInfo(fullPath).GetFiles("*.*", SearchOption.AllDirectories).ToImmutableArray();
                        var filesCataloged = 0;
                        var filesToCatalog = modsDirectoryFiles.Length;
                        ProgressValue = 0;
                        ProgressMax = filesToCatalog;
                        platformFunctions.ProgressState = AppProgressState.Normal;
                        platformFunctions.ProgressMaximum = progressMax!.Value;
                        var preservedModFilePaths = new ConcurrentBag<string>();
                        var preservedFileOfInterestPaths = new ConcurrentBag<string>();
                        using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2)))
                        {
                            var timeAtCataloged = new ConcurrentDictionary<int, DateTimeOffset>();
                            await Task.WhenAll(modsDirectoryFiles.Select(async fileInfo =>
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
                        var now = DateTimeOffset.Now;
                        await pbDbContext.ModFiles
                            .Where(md => md.Path != null && md.Path.StartsWith(path) && !preservedModFilePaths.Contains(md.Path))
                            .ExecuteDeleteAsync()
                            .ConfigureAwait(false);
                        await pbDbContext.FilesOfInterest
                            .Where(foi => foi.Path.StartsWith(filesOfInterestPath) && !preservedFileOfInterestPaths.Contains(foi.Path))
                            .ExecuteDeleteAsync()
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var now = DateTimeOffset.Now;
                        var modFilesRemoved = await pbDbContext.ModFiles
                            .Where(md => md.Path != null && md.Path.StartsWith(path))
                            .ExecuteDeleteAsync()
                            .ConfigureAwait(false);
                        modFilesRemoved.ToString();
                        await pbDbContext.FilesOfInterest
                            .Where(foi => foi.Path.StartsWith(filesOfInterestPath))
                            .ExecuteDeleteAsync()
                            .ConfigureAwait(false);
                    }
                }
                EstimatedStateTimeRemaining = null;
                await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
                State = ModsDirectoryCatalogerState.AnalyzingTopology;
                ProgressMax = null;
                platformFunctions.ProgressState = AppProgressState.Indeterminate;
                var resourceWasRemovedOrReplaced = false;
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
                    		    mf.Path IS NOT NULL
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
        if (path is SmartSimObserver.GlobalModsManifestPackageName)
            return;
        try
        {
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.ScriptArchive)
            {
                ModFile? modFile = null;
                ModFileHash? modFileHash = null;
                var creation = (DateTimeOffset)fileInfo.CreationTimeUtc;
                var lastWrite = (DateTimeOffset)fileInfo.LastWriteTimeUtc;
                var size = fileInfo.Length;
                modFile = await pbDbContext.ModFiles.Include(mf => mf.ModFileHash).FirstOrDefaultAsync
                (
                    mf =>
                        mf.Path == path
                    && mf.Creation == creation
                    && mf.LastWrite == lastWrite
                    && mf.Size == size
                ).ConfigureAwait(false);
                if (modFile is not null)
                {
                    modFileHash = modFile.ModFileHash;
                    fileType = modFile.FileType;
                }
                if (modFile is null)
                {
                    var hash = await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false);
                    if (modFileHash is null)
                        (modFileHash, fileType) = await GetModFileHashAsync(pbDbContext, fileType, hash).ConfigureAwait(false);
                    var success = false;
                    (success, fileType) = await CatalogResourcesAndManifestsAsync(pbDbContext, fileInfo, modFileHash, fileType).ConfigureAwait(false);
                    if (!success)
                        return;
                    modFile = modFileHash.ModFiles?.Where(mf => mf.Path?.Equals(path, platformFunctions.FileSystemStringComparison) ?? false).FirstOrDefault();
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
                    if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.CorruptPackage)
                    {
                        ++PackageCount;
                        ResourceCount += modFileHash.Resources?.Count
                            ?? await pbDbContext.ModFileResources.CountAsync(mfr => mfr.ModFileHashId == modFileHash.Id).ConfigureAwait(false);
                    }
                    else if (fileType is ModsDirectoryFileType.ScriptArchive or ModsDirectoryFileType.CorruptScriptArchive)
                    {
                        ++ScriptArchiveCount;
                        PythonByteCodeFileCount += modFileHash.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".pyc"));
                        PythonScriptCount += modFileHash.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".py"));
                    }
                }
                modFile.Creation = creation;
                modFile.LastWrite = lastWrite;
                modFile.Size = size;
                modFile.FileType = fileType;
                if (pbDbContext.Entry(modFile).State is EntityState.Added)
                    await pbDbContext.ModFiles.Where(mf => mf.Path == path).ExecuteDeleteAsync().ConfigureAwait(false);
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unexpected exception encountered while processing {FilePath}", path);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.ModsDirectoryCataloger_Warning_CannotReadModFile, path, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
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
            .CountAsync(mf => mf.Path != null && (mf.FileType == ModsDirectoryFileType.Package || mf.FileType == ModsDirectoryFileType.CorruptPackage))
            .ConfigureAwait(false);
        PythonByteCodeFileCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.Name.EndsWith(".pyc")))
            .ConfigureAwait(false);
        PythonScriptCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash.ScriptModArchiveEntries.Count(smae => smae.Name.EndsWith(".py")))
            .ConfigureAwait(false);
        ResourceCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash.Resources.Count)
            .ConfigureAwait(false);
        ScriptArchiveCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.Path != null && (mf.FileType == ModsDirectoryFileType.ScriptArchive || mf.FileType == ModsDirectoryFileType.CorruptScriptArchive))
            .ConfigureAwait(false);
    }

    public Task WaitForBusyAsync(CancellationToken cancellationToken = default) =>
        busyManualResetEvent.WaitAsync(cancellationToken);

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default) =>
        idleManualResetEvent.WaitAsync(cancellationToken);

    public void WakeUp() =>
        awakeManualResetEvent.Set();
}
