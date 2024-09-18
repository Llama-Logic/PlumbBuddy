using static ZXing.QrCode.Internal.Mode;

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
                    Url = requiredMod.Url,
                    Version = requiredMod.Version
                });
            modManifest.RequiredMods = requiredMods;
        }
        return modManifest;
    }

    static readonly PropertyChangedEventArgs packageCountPropertyChangedEventArgs = new(nameof(PackageCount));
    static readonly PropertyChangedEventArgs resourceCountPropertyChangedEventArgs = new(nameof(ResourceCount));
    static readonly PropertyChangedEventArgs scriptArchiveCountPropertyChangedEventArgs = new(nameof(ScriptArchiveCount));
    static readonly PropertyChangedEventArgs statePropertyChangedEventArgs = new(nameof(State));
    static readonly TimeSpan fiveSeconds = TimeSpan.FromSeconds(5);

    public ModsDirectoryCataloger(ILifetimeScope lifetimeScope, ILogger<IModsDirectoryCataloger> logger, IPlatformFunctions platformFunctions, IPlayer player, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.lifetimeScope = lifetimeScope;
        this.logger = logger;
        this.platformFunctions = platformFunctions;
        this.player = player;
        this.superSnacks = superSnacks;
        awakeManualResetEvent = new(true);
        pathsProcessingQueue = new();
        saveChangesLock = new();
        Task.Run(UpdateAggregatePropertiesAsync);
        Task.Run(ProcessPathsQueueAsync);
    }

    readonly AsyncManualResetEvent awakeManualResetEvent;
    readonly ILifetimeScope lifetimeScope;
    readonly ILogger<IModsDirectoryCataloger> logger;
    int packageCount;
    readonly AsyncProducerConsumerQueue<string> pathsProcessingQueue;
    readonly IPlatformFunctions platformFunctions;
    readonly IPlayer player;
    int resourceCount;
    readonly AsyncLock saveChangesLock;
    int scriptArchiveCount;
    ModDirectoryCatalogerState state;
    readonly ISuperSnacks superSnacks;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int PackageCount
    {
        get => packageCount;
        private set
        {
            packageCount = value;
            PropertyChanged?.Invoke(this, packageCountPropertyChangedEventArgs);
        }
    }

    public int ResourceCount
    {
        get => resourceCount;
        private set
        {
            resourceCount = value;
            PropertyChanged?.Invoke(this, resourceCountPropertyChangedEventArgs);
        }
    }

    public int ScriptArchiveCount
    {
        get => scriptArchiveCount;
        private set
        {
            scriptArchiveCount = value;
            PropertyChanged?.Invoke(this, scriptArchiveCountPropertyChangedEventArgs);
        }
    }

    public ModDirectoryCatalogerState State
    {
        get => state;
        private set
        {
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
            State = ModDirectoryCatalogerState.Sleeping;
            await awakeManualResetEvent.WaitAsync().ConfigureAwait(false);
            State = ModDirectoryCatalogerState.Debouncing;
            var nomNom = new Queue<string>();
            nomNom.Enqueue(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
            while (true)
            {
                try
                {
                    await Task.Delay(fiveSeconds).ConfigureAwait(false);
                    State = ModDirectoryCatalogerState.Sleeping;
                    await awakeManualResetEvent.WaitAsync().ConfigureAwait(false);
                    State = ModDirectoryCatalogerState.Debouncing;
                    if (!await pathsProcessingQueue.OutputAvailableAsync(token).ConfigureAwait(false))
                        break;
                    while (await pathsProcessingQueue.OutputAvailableAsync(token).ConfigureAwait(false))
                    {
                        var path = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                        if (!nomNom.Contains(path))
                            nomNom.Enqueue(path);
                    }
                }
                catch (OperationCanceledException) // this was OutputAvailableAsync -- usually Mr. Cleary documents his throws 🙄
                {
                    // if we're here, it's because the processing queue is empty -- time to get to start eating
                    break;
                }
            }
            State = ModDirectoryCatalogerState.Cataloging;
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
                    var broadcastBlock = new BroadcastBlock<FileInfo>(fileInfo => fileInfo);
                    var actionBlock = new ActionBlock<FileInfo>
                    (
                        fileInfo => ProcessDequeuedFileAsync(modsDirectoryInfo, fileInfo),
                        new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }
                    );
                    broadcastBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });
                    var preservedModFilePaths = new List<string>();
                    var preservedFileOfInterestPaths = new List<string>();
                    foreach (var fileInfo in new DirectoryInfo(fullPath).GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        broadcastBlock.Post(fileInfo);
                        var fileType = GetFileType(fileInfo);
                        if (fileType is ModsDirectoryFileType.Package or ModsDirectoryFileType.ScriptArchive)
                            preservedModFilePaths.Add(fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]);
                        else if (fileType is not ModsDirectoryFileType.Ignored)
                            preservedFileOfInterestPaths.Add(fileInfo.FullName[(modsDirectoryInfo.FullName.Length + 1)..]);
                    }
                    broadcastBlock.Complete();
                    await actionBlock.Completion.ConfigureAwait(false);
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
            await UpdateAggregatePropertiesAsync(pbDbContext).ConfigureAwait(false);
            State = ModDirectoryCatalogerState.AnalyzingTopography;
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
                    SELECT
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
                    			JOIN ModFiles mf ON mf.Id = mfr.ModFileId
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
            State = ModDirectoryCatalogerState.Idle;
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
                    if (fileType is ModsDirectoryFileType.Package
                        && player.CacheStatus is SmartSimCacheStatus.Normal
                        && await pbDbContext.ModFiles.AnyAsync(mf => mf.Path == path).ConfigureAwait(false))
                        player.CacheStatus = SmartSimCacheStatus.Stale;
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
                            modFile = modFileHash.ModFiles?.FirstOrDefault();
                        }
                    }
                    else
                        modFile = modFileHash.ModFiles?.FirstOrDefault();
                }
                if (modFile is null)
                {
                    modFile = new() { ModFileHash = modFileHash };
                    (modFileHash!.ModFiles ??= []).Add(modFile);
                    if (fileType is ModsDirectoryFileType.Package)
                    {
                        using var dbpf = await DataBasePackedFile.FromPathAsync(fileInfo.FullName, forReadOnly: true).ConfigureAwait(false);
                        var keys = await dbpf.GetKeysAsync().ConfigureAwait(false);
                        modFile.Resources = keys.Select(key => new ModFileResource() { Key = key, ModFile = modFile }).ToList();
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
                            modFile.ModManifest = await TransformModFileManifestModelAsync(pbDbContext, saveChangesLock, modFileManifestModel).ConfigureAwait(false);
                            break;
                        }
                    }
                    else
                    {
                        try
                        {
                            using var archive = ZipFile.OpenRead(fileInfo.FullName);
                            if (archive.Entries.FirstOrDefault(entry => entry.FullName.Equals("manifest.yml", StringComparison.OrdinalIgnoreCase)) is { } manifestEntry)
                            {
                                using var manifestStream = manifestEntry.Open();
                                using var manifestReader = new StreamReader(manifestStream);
                                if (ModFileManifestModel.TryParse(manifestReader.ReadToEnd(), out var modFileManifestModel))
                                    modFile.ModManifest = await TransformModFileManifestModelAsync(pbDbContext, saveChangesLock, modFileManifestModel).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "failed to check for manifest in {ScriptArchivePath}", path);
                        }
                    }
                    await pbDbContext.AddAsync(modFile).ConfigureAwait(false);
                    if (fileType is ModsDirectoryFileType.Package)
                    {
                        ++PackageCount;
                        ResourceCount += modFile.Resources?.Count ?? 0;
                    }
                    else
                        ++ScriptArchiveCount;
                }
                else if (modFile.AbsenceNoticed is not null)
                {
                    modFile.AbsenceNoticed = null;
                    if (fileType is ModsDirectoryFileType.Package)
                    {
                        ++PackageCount;
                        ResourceCount += modFile.Resources?.Count ?? 0;
                    }
                    else
                        ++ScriptArchiveCount;
                }
                modFile.Path = path;
                modFile.Creation = creation;
                modFile.LastWrite = lastWrite;
                modFile.Size = size;
                modFile.FileType = fileType;
                if (modFile is not null && pbDbContext.Entry(modFile).State is EntityState.Added or EntityState.Modified)
                {
                    using var heldSaveChangesLock = await saveChangesLock.LockAsync().ConfigureAwait(false);
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
        ResourceCount = await pbDbContext.ModFileResources
            .CountAsync(mfr => mfr.ModFile!.Path != null)
            .ConfigureAwait(false);
        ScriptArchiveCount = await pbDbContext.ModFiles
            .CountAsync(mf => mf.Path != null && mf.FileType == ModsDirectoryFileType.ScriptArchive)
            .ConfigureAwait(false);
    }

    public void WakeUp() =>
        awakeManualResetEvent.Set();
}
