namespace PlumbBuddy.Services;

public class ModsDirectoryCataloger :
    IModsDirectoryCataloger
{
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

    static async IAsyncEnumerable<PackCode> TransformCodesEnumerableAsync(PbDbContext pbDbContext, AsyncLock saveChangesLock, IEnumerable<string> codes)
    {
        var existingPackCodes = await pbDbContext.PackCodes.Where(pc => codes.Contains(pc.Code)).ToDictionaryAsync(pc => pc.Code).ConfigureAwait(false);
        foreach (var code in codes)
        {
            if (existingPackCodes.TryGetValue(code, out var existingPackCode))
                yield return existingPackCode;
            var newPackCode = new PackCode { Code = code };
            await pbDbContext.PackCodes.AddAsync(newPackCode).ConfigureAwait(false);
            try
            {
                using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                pbDbContext.Entry(newPackCode).State = EntityState.Detached;
                newPackCode = await pbDbContext.PackCodes.FirstAsync(pc => pc.Code == code).ConfigureAwait(false);
            }
            existingPackCodes.Add(code, newPackCode);
            yield return newPackCode;
        }
    }

    static async IAsyncEnumerable<ModCreator> TransformCreatorNamesEnumerableAsync(PbDbContext pbDbContext, AsyncLock saveChangesLock, IEnumerable<string> names)
    {
        var existingModCreators = await pbDbContext.ModCreators.Where(mc => names.Contains(mc.Name)).ToDictionaryAsync(mc => mc.Name).ConfigureAwait(false);
        foreach (var name in names)
        {
            if (existingModCreators.TryGetValue(name, out var existingModCreator))
                yield return existingModCreator;
            var newModCreator = new ModCreator { Name = name };
            await pbDbContext.ModCreators.AddAsync(newModCreator).ConfigureAwait(false);
            try
            {
                using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                pbDbContext.Entry(newModCreator).State = EntityState.Detached;
                newModCreator = await pbDbContext.ModCreators.FirstAsync(mc => mc.Name == name).ConfigureAwait(false);
            }
            existingModCreators.Add(name, newModCreator);
            yield return newModCreator;
        }
    }

    static async IAsyncEnumerable<ModFeature> TransformFeatureNamesEnumerableAsync(PbDbContext pbDbContext, AsyncLock saveChangesLock, IEnumerable<string> names)
    {
        var existingModFeatures = await pbDbContext.ModFeatures.Where(mf => names.Contains(mf.Name)).ToDictionaryAsync(mf => mf.Name).ConfigureAwait(false);
        foreach (var name in names)
        {
            if (existingModFeatures.TryGetValue(name, out var existingModFeature))
                yield return existingModFeature;
            var newModFeature = new ModFeature { Name = name };
            await pbDbContext.ModFeatures.AddAsync(newModFeature).ConfigureAwait(false);
            try
            {
                using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                pbDbContext.Entry(newModFeature).State = EntityState.Detached;
                newModFeature = await pbDbContext.ModFeatures.FirstAsync(mf => mf.Name == name).ConfigureAwait(false);
            }
            existingModFeatures.Add(name, newModFeature);
            yield return newModFeature;
        }
    }

    static async IAsyncEnumerable<ModFileHash> TrasnformHashByteArraysEnumerableAsync(PbDbContext pbDbContext, AsyncLock saveChangesLock, IEnumerable<byte[]> hashes)
    {
        var existingModFileHashes = (await pbDbContext.ModFileHashes.Where(mfh => Enumerable.Contains(hashes, mfh.Sha256)).ToListAsync().ConfigureAwait(false)).ToDictionary(mfh => mfh.Sha256.ToHexString());
        foreach (var hash in hashes)
        {
            var hashHexStr = hash.ToHexString();
            if (existingModFileHashes.TryGetValue(hashHexStr, out var existingModFileHash))
                yield return existingModFileHash;
            var newModFileHash = new ModFileHash { Sha256 = hash };
            await pbDbContext.ModFileHashes.AddAsync(newModFileHash).ConfigureAwait(false);
            try
            {
                using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
            {
                pbDbContext.Entry(newModFileHash).State = EntityState.Detached;
                newModFileHash = await pbDbContext.ModFileHashes.FirstAsync(mfh => mfh.Sha256 == hash).ConfigureAwait(false);
            }
            existingModFileHashes.Add(hashHexStr, newModFileHash);
            yield return newModFileHash;
        }
    }

    static async Task<ModManifest> TransformModFileManifestModelAsync(PbDbContext pbDbContext, AsyncLock saveChangesLock, ModFileManifestModel modFileManifestModel)
    {
        var modManifest = new ModManifest
        {
            Creators = await TransformCreatorNamesEnumerableAsync(pbDbContext, saveChangesLock, modFileManifestModel.Creators).ToListAsync().ConfigureAwait(false),
            Features = await TransformFeatureNamesEnumerableAsync(pbDbContext, saveChangesLock, modFileManifestModel.Features).ToListAsync().ConfigureAwait(false),
            Name = modFileManifestModel.Name,
            RequiredPacks = await TransformCodesEnumerableAsync(pbDbContext, saveChangesLock, modFileManifestModel.RequiredPacks).ToListAsync().ConfigureAwait(false),
            SubsumedFiles = await TrasnformHashByteArraysEnumerableAsync(pbDbContext, saveChangesLock, modFileManifestModel.SubsumedFiles).ToListAsync().ConfigureAwait(false),
            Url = modFileManifestModel.Url,
            Version = modFileManifestModel.Version
        };
        if (modFileManifestModel.IntentionalOverrides.Count > 0)
        {
            var intentionalOverrides = new List<IntentionalOverride>();
            foreach (var intentionalOverride in modFileManifestModel.IntentionalOverrides)
                intentionalOverrides.Add(new IntentionalOverride
                {
                    Key = intentionalOverride.Key,
                    ModFiles = await TrasnformHashByteArraysEnumerableAsync(pbDbContext, saveChangesLock, intentionalOverride.ModFiles).ToListAsync().ConfigureAwait(false),
                    ModManifestKey = intentionalOverride.ModManifestKey,
                    ModName = intentionalOverride.ModName,
                    ModVersion = intentionalOverride.ModVersion,
                    Name = intentionalOverride.Name
                });
            modManifest.IntentionalOverrides = intentionalOverrides;
        }
        if (modFileManifestModel.RequiredMods.Count > 0)
        {
            var requiredMods = new List<RequiredMod>();
            foreach (var requiredMod in modFileManifestModel.RequiredMods)
                requiredMods.Add(new RequiredMod
                {
                    Creators = await TransformCreatorNamesEnumerableAsync(pbDbContext, saveChangesLock, requiredMod.Creators).ToListAsync().ConfigureAwait(false),
                    Files = await TrasnformHashByteArraysEnumerableAsync(pbDbContext, saveChangesLock, requiredMod.Files).ToListAsync().ConfigureAwait(false),
                    ManifestKey = requiredMod.ModManifestKey,
                    Name = requiredMod.Name,
                    RequiredFeatures = await TransformFeatureNamesEnumerableAsync(pbDbContext, saveChangesLock, requiredMod.RequiredFeatures).ToListAsync().ConfigureAwait(false),
                    Url = requiredMod.Url,
                    Version = requiredMod.Version
                });
            modManifest.RequiredMods = requiredMods;
        }
        return modManifest;
    }

    static readonly PropertyChangedEventArgs estimatedStateTimeRemainingPropertyChangedEventArgs = new(nameof(EstimatedStateTimeRemaining));
    static readonly PropertyChangedEventArgs packageCountPropertyChangedEventArgs = new(nameof(PackageCount));
    static readonly PropertyChangedEventArgs pythonByteCodeFileCountPropertyChangedEventArgs = new(nameof(PythonByteCodeFileCount));
    static readonly PropertyChangedEventArgs pythonScriptCountPropertyChangedEventArgs = new(nameof(PythonScriptCount));
    static readonly PropertyChangedEventArgs resourceCountPropertyChangedEventArgs = new(nameof(ResourceCount));
    static readonly PropertyChangedEventArgs scriptArchiveCountPropertyChangedEventArgs = new(nameof(ScriptArchiveCount));
    static readonly PropertyChangedEventArgs statePropertyChangedEventArgs = new(nameof(State));
    static readonly TimeSpan oneSecond = TimeSpan.FromSeconds(1);

    public ModsDirectoryCataloger(ILifetimeScope lifetimeScope, ILogger<IModsDirectoryCataloger> logger, IPlatformFunctions platformFunctions, ISynchronization synchronization, IPlayer player, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(synchronization);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.lifetimeScope = lifetimeScope;
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.superSnacks = superSnacks;
        awakeManualResetEvent = new(true);
        pathsProcessingQueue = new();
        saveChangesLock = synchronization.EntityFrameworkCoreDatabaseContextWriteLock;
        Task.Run(UpdateAggregatePropertiesAsync);
        Task.Run(ProcessPathsQueueAsync);
    }

    readonly AsyncManualResetEvent awakeManualResetEvent;
    TimeSpan? estimatedStateTimeRemaining;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<IModsDirectoryCataloger> logger;
    int packageCount;
    readonly AsyncProducerConsumerQueue<string> pathsProcessingQueue;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    int pythonByteCodeFileCount;
    int pythonScriptCount;
    int resourceCount;
    readonly AsyncLock saveChangesLock;
    int scriptArchiveCount;
    ModsDirectoryCatalogerState state;
    readonly ISuperSnacks superSnacks;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public void Catalog(string path) =>
        pathsProcessingQueue.Enqueue(path);

    public void GoToSleep() =>
        awakeManualResetEvent.Reset();

    async Task ProcessPathsQueueAsync()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();
        while (await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
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
                State = ModsDirectoryCatalogerState.Debouncing;
                try
                {
                    if (!await pathsProcessingQueue.OutputAvailableAsync(token).ConfigureAwait(false))
                        break;
                }
                catch (OperationCanceledException) // this was OutputAvailableAsync -- usually Mr. Cleary documents his throws 🙄
                {
                    // if we're here, it's because the processing queue is empty -- time to start eating
                    break;
                }
                try
                {
                    while (await pathsProcessingQueue.OutputAvailableAsync(token).ConfigureAwait(false))
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
            State = ModsDirectoryCatalogerState.Cataloging;
            using var nestedScope = lifetimeScope.BeginLifetimeScope();
            var pbDbContext = nestedScope.Resolve<PbDbContext>();
            while (nomNom.TryDequeue(out var path))
            {
                var modsDirectoryPath = Path.Combine(player.UserDataFolderPath, "Mods");
                var modsDirectoryInfo = new DirectoryInfo(modsDirectoryPath);
                var fullPath = Path.Combine(modsDirectoryPath, path);
                if (File.Exists(fullPath))
                    await ProcessDequeuedFileAsync(modsDirectoryInfo, new FileInfo(fullPath)).ConfigureAwait(false);
                else if (Directory.Exists(fullPath))
                {
                    var modsDirectoryFiles = new DirectoryInfo(fullPath).GetFiles("*.*", SearchOption.AllDirectories).ToImmutableArray();
                    var filesCataloged = 0;
                    var filesToCatalog = modsDirectoryFiles.Length;
                    var preservedModFilePaths = new ConcurrentBag<string>();
                    var preservedFileOfInterestPaths = new ConcurrentBag<string>();
                    using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2)))
                    {
                        var catalogingStarted = DateTimeOffset.Now;
                        await Task.WhenAll(modsDirectoryFiles.Select(async fileInfo =>
                        {
                            await semaphore.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                await ProcessDequeuedFileAsync(modsDirectoryInfo, fileInfo).ConfigureAwait(false);
                                var newFilesCataloged = Interlocked.Increment(ref filesCataloged);
                                EstimatedStateTimeRemaining = new TimeSpan((DateTimeOffset.Now - catalogingStarted).Ticks / newFilesCataloged * (filesToCatalog - newFilesCataloged) / 10000000 * 10000000 + 10000000);
                                var fileType = GetFileType(fileInfo);
                                if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.ScriptArchive)
                                    preservedModFilePaths.Add(fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]);
                                else if (fileType is not ModsDirectoryFileType.Ignored)
                                    preservedFileOfInterestPaths.Add(fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        })).ConfigureAwait(false);
                    }
                    using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                    await pbDbContext.ModFiles
                        .Where(md => md.Path != null && md.Path.StartsWith(path) && !preservedModFilePaths.Contains(md.Path))
                        .ExecuteUpdateAsync(u => u.SetProperty(mf => mf.Path, _ => null))
                        .ConfigureAwait(false);
                    await pbDbContext.FilesOfInterest
                        .Where(foi => foi.Path.StartsWith(path) && !preservedFileOfInterestPaths.Contains(foi.Path))
                        .ExecuteDeleteAsync()
                        .ConfigureAwait(false);
                }
                else
                {
                    using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                    var modFilesRemoved = await pbDbContext.ModFiles
                        .Where(md => md.Path != null && md.Path.StartsWith(path))
                        .ExecuteUpdateAsync(u => u.SetProperty(mf => mf.Path, _ => null))
                        .ConfigureAwait(false);
                    modFilesRemoved.ToString();
                    await pbDbContext.FilesOfInterest
                        .Where(foi => foi.Path.StartsWith(path))
                        .ExecuteDeleteAsync()
                        .ConfigureAwait(false);
                }
            }
            EstimatedStateTimeRemaining = null;
            await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
            State = ModsDirectoryCatalogerState.AnalyzingTopography;
            var resourceWasRemovedOrReplaced = false;
            var latestTopologySnapshot = await pbDbContext.TopologySnapshots.OrderByDescending(ts => ts.Id).FirstOrDefaultAsync().ConfigureAwait(false);
            using (var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false))
            {
                var currentTopologySnapshot = new TopologySnapshot { Taken = DateTimeOffset.UtcNow };
                await pbDbContext.TopologySnapshots.AddAsync(currentTopologySnapshot).ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                var pathCollation = platformFunctions.FileSystemStringComparison switch
                {
                    StringComparison.Ordinal => "BINARY",
                    StringComparison.OrdinalIgnoreCase => "NOCASE",
                    _ => throw new NotSupportedException($"Cannot translate {platformFunctions.FileSystemStringComparison} to SQLite collation")
                };
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
                    resourceWasRemovedOrReplaced = (await pbDbContext.Database.SqlQueryRaw<int>
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
            if (resourceWasRemovedOrReplaced && player.CacheStatus is SmartSimCacheStatus.Normal)
                player.CacheStatus = SmartSimCacheStatus.Stale;
            State = ModsDirectoryCatalogerState.Idle;
        }
    }

    async Task ProcessDequeuedFileAsync(DirectoryInfo modsDirectoryInfo, FileInfo fileInfo)
    {
        var fileType = GetFileType(fileInfo);
        if (fileType is ModsDirectoryFileType.Ignored)
            return;
        var path = fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..];
        try
        {
            using var nestedScope = lifetimeScope.BeginLifetimeScope();
            var pbDbContext = nestedScope.Resolve<PbDbContext>();
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
                    modFileHash = modFile.ModFileHash;
                if (modFile is null)
                {
                    var hash = await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false);
                    modFileHash = await pbDbContext.ModFileHashes.Include(mfh => mfh.ModFiles).FirstOrDefaultAsync(mfh => mfh.Sha256 == hash).ConfigureAwait(false);
                    if (modFileHash is null)
                    {
                        modFileHash = new() { Sha256 = hash };
                        await pbDbContext.ModFileHashes.AddAsync(modFileHash).ConfigureAwait(false);
                        try
                        {
                            using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                            await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                        }
                        catch (DbUpdateException dbUpdateEx) when (dbUpdateEx.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode is 19)
                        {
                            pbDbContext.Entry(modFileHash).State = EntityState.Detached;
                            modFileHash = await pbDbContext.ModFileHashes.Include(mfh => mfh.ModFiles).FirstAsync(mfh => mfh.Sha256 == hash).ConfigureAwait(false);
                        }
                    }
                    if (!modFileHash.ResourcesAndManifestCataloged)
                    {
                        if (fileType is ModsDirectoryFileType.Package)
                        {
                            using var dbpf = await DataBasePackedFile.FromPathAsync(fileInfo.FullName, forReadOnly: true).ConfigureAwait(false);
                            var keys = await dbpf.GetKeysAsync().ConfigureAwait(false);
                            modFileHash.Resources = keys.Select(key => new ModFileResource() { Key = key, ModFileHash = modFileHash }).ToList();
                            foreach (var snippetTuningKey in keys.Where(key => key.Type is ResourceType.SnippetTuning).OrderBy(key => key.Group).ThenBy(key => key.FullInstance))
                            {
                                ModFileManifestModel modFileManifestModel;
                                try
                                {
                                    modFileManifestModel = await dbpf.GetModFileManifestAsync(snippetTuningKey).ConfigureAwait(false);
                                }
                                catch (FileNotFoundException)
                                {
                                    logger.LogDebug("{PackagePath} :: {ResourceKey} snippet tuning marked as deleted", path, snippetTuningKey);
                                    continue;
                                }
                                catch (XmlException)
                                {
                                    logger.LogDebug("{PackagePath} :: {ResourceKey} snippet tuning not a mod file manifest", path, snippetTuningKey);
                                    continue;
                                }
                                modFileHash.ModManifest = await TransformModFileManifestModelAsync(pbDbContext, saveChangesLock, modFileManifestModel).ConfigureAwait(false);
                                break;
                            }
                        }
                        else
                        {
                            try
                            {
                                using var archive = ZipFile.OpenRead(fileInfo.FullName);
                                var scriptModArchiveEntries = new List<ScriptModArchiveEntry>();
                                foreach (var entry in archive.Entries)
                                {
                                    scriptModArchiveEntries.Add(new()
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
                                    if (entry.FullName.Equals("manifest.yml", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            using var manifestStream = entry.Open();
                                            using var manifestReader = new StreamReader(manifestStream);
                                            if (ModFileManifestModel.TryParse(manifestReader.ReadToEnd(), out var modFileManifestModel))
                                                modFileHash.ModManifest = await TransformModFileManifestModelAsync(pbDbContext, saveChangesLock, modFileManifestModel).ConfigureAwait(false);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogWarning(ex, "failed to read manifest in {ScriptArchivePath}", path);
                                        }
                                    }
                                }
                                modFileHash.ScriptModArchiveEntries = scriptModArchiveEntries;
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "failed to analyze script mod at {ScriptArchivePath}", path);
                            }
                        }
                        modFileHash.ResourcesAndManifestCataloged = true;
                    }
                    modFile = modFileHash.ModFiles?.Where(mf => mf.Path?.Equals(path, platformFunctions.FileSystemStringComparison) ?? false).FirstOrDefault();
                }
                if (modFile is null)
                {
                    modFile = new() { ModFileHash = modFileHash };
                    (modFileHash!.ModFiles ??= []).Add(modFile);
                    await pbDbContext.AddAsync(modFile).ConfigureAwait(false);
                    if (fileType is ModsDirectoryFileType.Package)
                    {
                        ++PackageCount;
                        ResourceCount += modFileHash.Resources?.Count
                            ?? await pbDbContext.ModFileResources.CountAsync(mfr => mfr.ModFileHashId == modFileHash.Id).ConfigureAwait(false);
                    }
                    else
                    {
                        ++ScriptArchiveCount;
                        PythonByteCodeFileCount += modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".pyc"));
                        PythonScriptCount += modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".py"));
                    }
                }
                else if (modFile.AbsenceNoticed is not null)
                {
                    modFile.AbsenceNoticed = null;
                    if (fileType is ModsDirectoryFileType.Package)
                    {
                        --PackageCount;
                        ResourceCount -= modFileHash!.Resources?.Count
                            ?? await pbDbContext.ModFileResources.CountAsync(mfr => mfr.ModFileHashId == modFileHash.Id).ConfigureAwait(false);
                    }
                    else
                    {
                        --ScriptArchiveCount;
                        PythonByteCodeFileCount -= modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".pyc"));
                        PythonScriptCount -= modFileHash!.ScriptModArchiveEntries?.Count(smae => smae.Name.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
                            ?? await pbDbContext.ScriptModArchiveEntries.CountAsync(smae => smae.ModFileHashId == modFileHash.Id && smae.Name.EndsWith(".py"));
                    }
                }
                modFile.Path = path;
                modFile.Creation = creation;
                modFile.LastWrite = lastWrite;
                modFile.Size = size;
                modFile.FileType = fileType;
                if (pbDbContext.Entry(modFile).State is EntityState.Added or EntityState.Modified)
                {
                    using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                    if (pbDbContext.Entry(modFile).State is EntityState.Added)
                        await pbDbContext.ModFiles.Where(mf => mf.Path == path).ExecuteUpdateAsync(u => u.SetProperty(mf => mf.Path, _ => null)).ConfigureAwait(false);
                    await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            else if (fileType is not ModsDirectoryFileType.Ignored && !await pbDbContext.FilesOfInterest.AnyAsync(foi => foi.Path == path).ConfigureAwait(false))
            {
                pbDbContext.FilesOfInterest.Add(new FileOfInterest
                {
                    FileType = fileType,
                    Path = path
                });
                using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
                await pbDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unexpected exception encountered while processing {FilePath}", path);
            superSnacks.OfferRefreshments(new MarkupString(
                $"""
                <h3>Whoops!</h3>
                I ran into a problem trying to catalog the package at this location in your Mods folder:<br />
                <strong>{path}</strong><br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{ex.GetType().Name}: {ex.Message}</span><br />
                <br />
                There is more detailed technical information available in the log I write to the PlumbBuddy folder in your Documents.
                """), Severity.Warning, options =>
                {
                    options.RequireInteraction = true;
                    options.Icon = MaterialDesignIcons.Normal.PackageVariantRemove;
                });
        }
    }

    async Task UpdateAggregatePropertiesAsync()
    {
        using var nestedScope = lifetimeScope.BeginLifetimeScope();
        var pbDbContext = nestedScope.Resolve<PbDbContext>();
        await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
    }

    async Task UpdateAggregatePropertiesAsync(PbDbContext pbDbContext)
    {
        PackageCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.Path != null && mf.FileType == ModsDirectoryFileType.Package)
            .ConfigureAwait(false);
        PythonByteCodeFileCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash!.ScriptModArchiveEntries!.Count(smae => smae.Name.EndsWith(".pyc")))
            .ConfigureAwait(false);
        PythonScriptCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash!.ScriptModArchiveEntries!.Count(smae => smae.Name.EndsWith(".py")))
            .ConfigureAwait(false);
        ResourceCount = await pbDbContext.ModFiles
            .Where(mf => mf.Path != null)
            .SumAsync(mf => mf.ModFileHash!.Resources!.Count)
            .ConfigureAwait(false);
        ScriptArchiveCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.Path != null && mf.FileType == ModsDirectoryFileType.ScriptArchive)
            .ConfigureAwait(false);
    }

    public void WakeUp() =>
        awakeManualResetEvent.Set();
}
