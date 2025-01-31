using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
[SuppressMessage("Naming", "CA1724: Type names should not match namespaces")]
public partial class Archivist :
    IArchivist
{
    static readonly ImmutableHashSet<string> extensions = Enumerable.Range(0, 5)
        .Select(i => $".ver{i}")
        .Append(".save")
        .Order()
        .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    static readonly TimeSpan oneQuarterSecond = TimeSpan.FromSeconds(0.25);

    [GeneratedRegex(@"^N\-(?<nucleusIdHex>[\da-f]{16})\-C\-(?<createdHex>[\da-f]{16})\.chronicle\.sqlite$")]
    private static partial Regex GetChronicleDatabaseFileNamePattern();

    [GeneratedRegex(@"^Slot_(?<slot>[\da-f]{8})\.save(?<ver>\.ver[0-4])?$")]
    private static partial Regex GetSavesDirectoryLegalFilenamePattern();

    public Archivist(ILoggerFactory loggerFactory, ILogger<Archivist> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, IAppLifecycleManager appLifecycleManager, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.loggerFactory = loggerFactory;
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.appLifecycleManager = appLifecycleManager;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.superSnacks = superSnacks;
        chronicleByNucleusIdAndCreated = [];
        chronicles = [];
        Chronicles = new(chronicles);
        chroniclesLock = new();
        connectionLock = new();
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        if (this.settings.ArchivistEnabled)
            ConnectToFolders();
    }

    ~Archivist() =>
        Dispose(false);

    readonly IAppLifecycleManager appLifecycleManager;
    readonly Dictionary<(ulong nucleusId, ulong created), Chronicle> chronicleByNucleusIdAndCreated;
    readonly ObservableCollection<Chronicle> chronicles;
    readonly AsyncLock chroniclesLock;
    string chroniclesSearchText = string.Empty;
    readonly AsyncLock connectionLock;
    string? diagnosticStatus;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? fileSystemWatcher;
    readonly ILoggerFactory loggerFactory;
    bool isDisposed;
    readonly ILogger<Archivist> logger;
    readonly IModsDirectoryCataloger modsDirectoryCataloger;
    AsyncProducerConsumerQueue<string>? pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    Chronicle? selectedChronicle;
    readonly ISettings settings;
    string snapshotsSearchText = string.Empty;
    ArchivistState state;
    readonly ISuperSnacks superSnacks;

    public ReadOnlyObservableCollection<Chronicle> Chronicles { get; }

    public string ChroniclesSearchText
    {
        get => chroniclesSearchText;
        set
        {
            if (chroniclesSearchText == value)
                return;
            chroniclesSearchText = value;
            OnPropertyChanged();
        }
    }

    public string? DiagnosticStatus
    {
        get => diagnosticStatus;
        private set
        {
            if (diagnosticStatus == value)
                return;
            diagnosticStatus = value;
            OnPropertyChanged();
        }
    }

    public Chronicle? SelectedChronicle
    {
        get => selectedChronicle;
        set
        {
            selectedChronicle = value;
            OnPropertyChanged();
        }
    }

    public string SnapshotsSearchText
    {
        get => snapshotsSearchText;
        set
        {
            if (snapshotsSearchText == value)
                return;
            snapshotsSearchText = value;
            OnPropertyChanged();
        }
    }

    public ArchivistState State
    {
        get => state;
        private set
        {
            if (state == value)
                return;
            state = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task AddPathToProcessAsync(FileSystemInfo fileSystemInfo)
    {
        ArgumentNullException.ThrowIfNull(fileSystemInfo);
        return pathsProcessingQueue is null
            ? Task.CompletedTask
            : pathsProcessingQueue.EnqueueAsync(fileSystemInfo.FullName);
    }

    void ConnectToFolders() =>
        Task.Run(ConnectToFoldersAsync);

    async Task ConnectToFoldersAsync()
    {
        using var connectionLockHeld = await connectionLock.LockAsync().ConfigureAwait(false);
        if (!settings.ArchivistEnabled || fileSystemWatcher is not null)
            return;
        var savesFolder = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"));
        if (!savesFolder.Exists)
            return;
        DirectoryInfo archiveFolder;
        try
        {
            archiveFolder = new DirectoryInfo(settings.ArchiveFolderPath);
            if (!archiveFolder.Exists)
            {
                var creationStack = new Stack<DirectoryInfo>();
                creationStack.Push(archiveFolder);
                var archiveAncestorFolder = archiveFolder.Parent;
                while (archiveAncestorFolder is not null && !archiveAncestorFolder.Exists)
                {
                    creationStack.Push(archiveAncestorFolder);
                    archiveAncestorFolder = archiveAncestorFolder.Parent;
                }
                while (creationStack.TryPop(out var folderToCreate))
                    folderToCreate.Create();
            }
        }
        catch (Exception ex)
        {
            await RouteCriticalExceptionAsync(ex).ConfigureAwait(false);
            return;
        }
        pathsProcessingQueue = new();
        fileSystemWatcher = new FileSystemWatcher(Path.Combine(settings.UserDataFolderPath, "saves"))
        {
            IncludeSubdirectories = false,
            NotifyFilter =
                  NotifyFilters.CreationTime
                | NotifyFilters.DirectoryName
                | NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
        };
        fileSystemWatcher.Changed += HandleFileSystemWatcherChanged;
        fileSystemWatcher.Created += HandleFileSystemWatcherCreated;
        fileSystemWatcher.Error += HandleFileSystemWatcherError;
        fileSystemWatcher.Renamed += HandleFileSystemWatcherRenamed;
        fileSystemWatcher.EnableRaisingEvents = true;
        _ = Task.Run(ProcessPathsQueueAsync);
        using (var chroniclesLockHeld = await chroniclesLock.LockAsync().ConfigureAwait(false))
        {
            foreach (var fileInfoAndMatch in archiveFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                .Select(fileInfo => new
                {
                    FileInfo = fileInfo,
                    Match = GetChronicleDatabaseFileNamePattern().Match(fileInfo.Name)
                })
                .Where(fileInfoAndMatch => fileInfoAndMatch.Match.Success))
            {
                IDbContextFactory<ChronicleDbContext> chronicleDbContextFactory = new ChronicleDbContextFactory(fileInfoAndMatch.FileInfo);
                using var chronicleDbContext = await chronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if ((await chronicleDbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                    await chronicleDbContext.Database.MigrateAsync().ConfigureAwait(false);
                var chronicle = new Chronicle(loggerFactory, loggerFactory.CreateLogger<Chronicle>(), chronicleDbContextFactory, this);
                chronicleByNucleusIdAndCreated.Add((ulong.Parse(fileInfoAndMatch.Match.Groups["nucleusIdHex"].Value, NumberStyles.HexNumber), ulong.Parse(fileInfoAndMatch.Match.Groups["createdHex"].Value, NumberStyles.HexNumber)), chronicle);
                await chronicle.FirstLoadComplete.ConfigureAwait(false);
                chronicles.Add(chronicle);
            }
        }
        if (settings.ArchivistAutoIngestSaves)
            await pathsProcessingQueue.EnqueueAsync(Path.Combine(settings.UserDataFolderPath, "saves")).ConfigureAwait(false);
    }

    void DisconnectFromFolders() =>
        Task.Run(DisconnectFromFoldersAsync);

    async Task DisconnectFromFoldersAsync()
    {
        using var connectionLockHeld = await connectionLock.LockAsync().ConfigureAwait(false);
        if (fileSystemWatcher is not null)
        {
            fileSystemWatcher.Changed -= HandleFileSystemWatcherChanged;
            fileSystemWatcher.Created -= HandleFileSystemWatcherCreated;
            fileSystemWatcher.Error -= HandleFileSystemWatcherError;
            fileSystemWatcher.Renamed -= HandleFileSystemWatcherRenamed;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }
        pathsProcessingQueue?.CompleteAdding();
        pathsProcessingQueue = null;
        using var chroniclesLockHeld = await chroniclesLock.LockAsync().ConfigureAwait(false);
        chronicleByNucleusIdAndCreated.Clear();
        chronicles.Clear();
        SelectedChronicle = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing && !isDisposed)
        {
            DisconnectFromFolders();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            isDisposed = true;
        }
    }

    void HandleFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
    {
        if (fileSystemWatcher is not null
            && new FileInfo(e.FullPath) is { } fileInfo
            && extensions.Contains(fileInfo.Extension)
            && fileInfo.Exists)
            pathsProcessingQueue?.Enqueue(e.FullPath);
    }

    void HandleFileSystemWatcherCreated(object sender, FileSystemEventArgs e)
    {
        if (fileSystemWatcher is not null
            && new FileInfo(e.FullPath) is { } fileInfo
            && extensions.Contains(fileInfo.Extension)
            && fileInfo.Exists)
            pathsProcessingQueue?.Enqueue(e.FullPath);
    }

    void HandleFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError(e.GetException(), "saves directory monitoring encountered unexpected unhandled exception");
        ReconnectFolders();
    }

    void HandleFileSystemWatcherRenamed(object sender, RenamedEventArgs e)
    {
        if (fileSystemWatcher is not null
            && new FileInfo(e.FullPath) is { } fileInfo
            && extensions.Contains(fileInfo.Extension)
            && fileInfo.Exists)
            pathsProcessingQueue?.Enqueue(e.FullPath);
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ArchiveFolderPath)
            or nameof(ISettings.UserDataFolderPath))
            ReconnectFolders();
        else if (e.PropertyName is nameof(ISettings.ArchivistAutoIngestSaves))
        {
            if (settings.ArchivistAutoIngestSaves)
                pathsProcessingQueue?.Enqueue(Path.Combine(settings.UserDataFolderPath, "saves"));
        }
        else if (e.PropertyName is nameof(ISettings.ArchivistEnabled))
        {
            if (settings.ArchivistEnabled)
                ConnectToFolders();
            else
                DisconnectFromFolders();
        }
    }

    public async Task LoadChronicleAsync(Chronicle chronicle)
    {
        ArgumentNullException.ThrowIfNull(chronicle);
        await chronicle.FirstLoadComplete.ConfigureAwait(false);
        using var chroniclesLockHeld = await chroniclesLock.LockAsync().ConfigureAwait(false);
        chronicleByNucleusIdAndCreated.Add((chronicle.NucleusId, chronicle.Created), chronicle);
        chronicles.Add(chronicle);
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task ProcessDequeuedFileAsync(FileInfo fileInfo, bool isInSavesDirectory, bool isSingleEnqueuedFile, bool gameWasRunningAtStart)
    {
        DataBasePackedFile? package = null;
        try
        {
            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Wait";
            var wasLive =
                   isInSavesDirectory
                && isSingleEnqueuedFile
                && gameWasRunningAtStart;
            fileInfo.Refresh();
            var length = fileInfo.Length;
            while (gameWasRunningAtStart)
            {
                await Task.Delay(oneQuarterSecond).ConfigureAwait(false);
                fileInfo.Refresh();
                var newLength = fileInfo.Length;
                if (length == newLength)
                    break;
                length = newLength;
            }
            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / SHA256 Package";
            var fileHash = await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false);
            if (chronicles.Any(c => c.Snapshots.Any(s => s.OriginalPackageSha256.SequenceEqual(fileHash) || s.EnhancedPackageSha256.SequenceEqual(fileHash))))
                return;
            var fileHashArray = Unsafe.As<ImmutableArray<byte>, byte[]>(ref fileHash);
            package = await DataBasePackedFile.FromPathAsync(fileInfo.FullName).ConfigureAwait(false);
            var packageKeys = await package.GetKeysAsync().ConfigureAwait(false);
            var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
            if (saveGameDataKeys.Length is <= 0 or >= 2)
                throw new FormatException("save package contains invalid number of save game data resources");
            var saveGameDataKey = saveGameDataKeys[0];
            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Deserialize SGD";
            var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await package.GetAsync(saveGameDataKey).ConfigureAwait(false));
            if (saveGameData.SaveSlot is not { } saveSlot
                || saveGameData.Account is not { } account)
                return;
            IDbContextFactory<ChronicleDbContext> chronicleDbContextFactory = new ChronicleDbContextFactory(new FileInfo(Path.Combine(settings.ArchiveFolderPath, $"N-{account.NucleusId:x16}-C-{account.Created:x16}.chronicle.sqlite")));
            using var chronicleDbContext = await chronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if ((await chronicleDbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                await chronicleDbContext.Database.MigrateAsync().ConfigureAwait(false);
            SavePackageSnapshot? newSnapshot = null;
            if (await chronicleDbContext.KnownSavePackageHashes
                .AnyAsync(sps => sps.Sha256 == fileHashArray)
                .ConfigureAwait(false))
                return;
            var chroniclePropertySet = await chronicleDbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false);
            if (chroniclePropertySet is null)
            {
                var nucleusId = account.NucleusId;
                var nucleusIdBytes = new byte[8];
                Span<byte> nucleusIdBytesSpan = nucleusIdBytes;
                MemoryMarshal.Write(nucleusIdBytesSpan, in nucleusId);
                var created = account.Created;
                var createdBytes = new byte[8];
                Span<byte> createdBytesSpan = createdBytes;
                MemoryMarshal.Write(createdBytesSpan, in created);
                chroniclePropertySet = new ChroniclePropertySet
                {
                    Created = createdBytes,
                    Name = saveSlot.SlotName,
                    NucleusId = nucleusIdBytes,
                };
                await chronicleDbContext.ChroniclePropertySets.AddAsync(chroniclePropertySet).ConfigureAwait(false);
            }
            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Last Snapshot";
            var lastSnapshot = await chronicleDbContext.SavePackageSnapshots
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            var activeHousehold = saveGameData.Households.FirstOrDefault(h => h.HouseholdId == saveSlot.ActiveHouseholdId);
            var zoneId = saveSlot.GameplayData?.CameraData?.ZoneId ?? default;
            var lastZone = saveGameData.Zones?.FirstOrDefault(z => z.ZoneId == zoneId);
            var neighborhoodId = lastZone?.NeighborhoodId ?? default;
            var lastNeighborhood = saveGameData.Neighborhoods?.FirstOrDefault(n => n.NeighborhoodId == neighborhoodId);
            newSnapshot = new SavePackageSnapshot
            {
                ActiveHouseholdName = activeHousehold?.Name,
                LastPlayedLotName = lastZone?.Name,
                LastPlayedWorldName = lastNeighborhood?.Name,
                LastWriteTime = fileInfo.LastWriteTime,
                Label = $"Snapshot {(lastSnapshot is null ? 0 : lastSnapshot.Id) + 1:n0}",
                OriginalSavePackageHash = await chronicleDbContext.KnownSavePackageHashes.FirstOrDefaultAsync(esp => esp.Sha256 == fileHashArray).ConfigureAwait(false) ?? new() { Sha256 = fileHashArray },
                Resources = []
            };
            var contextLock = new AsyncLock();
            using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4)))
                await Task.WhenAll(packageKeys.Select(async key =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var keyBytes = new byte[16];
                        Span<byte> keyBytesSpan = keyBytes;
                        var type = key.Type;
                        MemoryMarshal.Write(keyBytesSpan[0..4], in type);
                        var group = key.Group;
                        MemoryMarshal.Write(keyBytesSpan[4..8], in group);
                        var fullInstance = key.FullInstance;
                        MemoryMarshal.Write(keyBytesSpan[8..16], in fullInstance);
                        var explicitCompressionMode = package.GetExplicitCompressionMode(key);
                        var content =
                              explicitCompressionMode is LlamaLogic.Packages.CompressionMode.CallerSuppliedStreamable
                            ? await package.GetRawAsync(key).ConfigureAwait(false)
                            : await package.GetAsync(key).ConfigureAwait(false);
                        var compressedContent = (await DataBasePackedFile.ZLibCompressAsync(content).ConfigureAwait(false)).ToArray();
                        var compressionType = explicitCompressionMode switch
                        {
                            LlamaLogic.Packages.CompressionMode.ForceOff => SavePackageResourceCompressionType.None,
                            LlamaLogic.Packages.CompressionMode.SetDeletedFlag => SavePackageResourceCompressionType.Deleted,
                            LlamaLogic.Packages.CompressionMode.ForceInternal => SavePackageResourceCompressionType.Internal,
                            LlamaLogic.Packages.CompressionMode.CallerSuppliedStreamable => SavePackageResourceCompressionType.Streamable,
                            LlamaLogic.Packages.CompressionMode.ForceZLib => SavePackageResourceCompressionType.ZLIB,
                            _ => throw new NotSupportedException("unsupported DBPF resource compression")
                        };
                        if (type is ResourceType.SaveGameHouseholdThumbnail)
                        {
                            try
                            {
                                var png = await package.GetTranslucentJpegAsPngAsync(key).ConfigureAwait(false);
                                newSnapshot.Thumbnail = MemoryMarshal.TryGetArray(png, out var segment) && segment.Array is { } array
                                    ? array
                                    : png.ToArray();
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "unexpected exception encountered while processing {FilePath}::{Key}", fileInfo.FullName, key);
                            }
                        }
                        SavePackageResource? resource = null;
                        if (lastSnapshot is not null)
                        {
                            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Load Prev {key}";
                            using var supplementaryChronicleDbContext = await chronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                            resource = await supplementaryChronicleDbContext.SavePackageResources.FirstOrDefaultAsync(spr => spr.Snapshots!.Any(sps => sps.Id == lastSnapshot.Id) && spr.Key == keyBytes).ConfigureAwait(false);
                            if (resource is not null)
                            {
                                supplementaryChronicleDbContext.Entry(resource).State = EntityState.Detached;
                                using var heldContextLockForAttach = await contextLock.LockAsync().ConfigureAwait(false);
                                chronicleDbContext.Update(resource);
                            }
                        }
                        if (resource is not null)
                        {
                            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Delta Gen {key}";
                            var previousContent = await DataBasePackedFile.ZLibDecompressAsync(resource.ContentZLib, resource.ContentSize).ConfigureAwait(false);
                            var previousCompressionType = resource.CompressionType;
                            if (!previousContent.Span.SequenceEqual(content.Span))
                            {
                                using var patchStream = new MemoryStream();
                                BinaryPatch.Create(content.Span, previousContent.Span, patchStream);
                                ReadOnlyMemory<byte> patch = patchStream.ToArray();
                                resource.ContentZLib = compressedContent;
                                resource.ContentSize = content.Length;
                                resource.CompressionType = compressionType;
                                DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Comp Delta {key}";
                                var patchZlib = (await DataBasePackedFile.ZLibCompressAsync(patch).ConfigureAwait(false)).ToArray();
                                using var heldContextLockForAddDelta = await contextLock.LockAsync().ConfigureAwait(false);
                                await chronicleDbContext.ResourceSnapshotDeltas.AddAsync
                                (
                                    new ResourceSnapshotDelta
                                    {
                                        PatchZLib = patchZlib,
                                        PatchSize = patch.Length,
                                        CompressionType = previousCompressionType,
                                        SavePackageResource = resource,
                                        SavePackageSnapshot = newSnapshot
                                    }
                                );
                            }
                        }
                        else
                            resource = new SavePackageResource
                            {
                                Key = keyBytes,
                                CompressionType = compressionType,
                                ContentZLib = compressedContent,
                                ContentSize = content.Length,
                            };
                        using var heldContextLockForAddResource = await contextLock.LockAsync().ConfigureAwait(false);
                        newSnapshot.Resources!.Add(resource);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })).ConfigureAwait(false);
            if (wasLive)
            {
                DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Live";
                newSnapshot.WasLive = true;
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await foreach (var modFile in pbDbContext.ModFiles
                    .Where(mf => mf.FileType == ModsDirectoryFileType.Package || mf.FileType == ModsDirectoryFileType.ScriptArchive)
                    .Include(mf => mf.ModFileHash)
                    .AsAsyncEnumerable())
                {
                    var modFileLastWrite = modFile.LastWrite ?? default;
                    var modFilePath = modFile.Path;
                    var modFileSha256 = modFile.ModFileHash!.Sha256;
                    var modFileSize = modFile.Size ?? default;
                    var snapshotModFile = await chronicleDbContext.SnapshotModFiles
                        .FirstOrDefaultAsync
                        (
                            smf =>
                            smf.LastWriteTime == modFileLastWrite
                            && smf.Path == modFilePath
                            && smf.Sha256 == modFileSha256
                            && smf.Size == modFileSize
                        )
                        .ConfigureAwait(false);
                    if (snapshotModFile is null)
                    {
                        snapshotModFile = new SnapshotModFile
                        {
                            LastWriteTime = modFileLastWrite,
                            Path = modFilePath,
                            Sha256 = modFileSha256,
                            Size = modFileSize
                        };
                        await chronicleDbContext.SnapshotModFiles.AddAsync(snapshotModFile).ConfigureAwait(false);
                    }
                    (snapshotModFile.Snapshots ??= []).Add(newSnapshot);
                }
            }
            MemoryStream? enhancedPackageMemoryStream = null;
            ReadOnlyMemory<byte> customThumbnail = chroniclePropertySet.Thumbnail;
            if (isInSavesDirectory
                && (!string.IsNullOrWhiteSpace(chroniclePropertySet.GameNameOverride)
                || !customThumbnail.IsEmpty))
            {
                DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Customize";
                if (chroniclePropertySet.GameNameOverride?.Trim() is { Length: > 0 } gameNameOverride
                    && saveSlot.SlotName != gameNameOverride)
                {
                    saveSlot.SlotName = gameNameOverride;
                    await package.SetAsync(saveGameDataKey, saveGameData.ToProtobufMessage(), package.GetExplicitCompressionMode(saveGameDataKey)).ConfigureAwait(false);
                }
                if (!customThumbnail.IsEmpty)
                    foreach (var saveThumbnail4Key in packageKeys.Where(key => key.Type is ResourceType.SaveGameHouseholdThumbnail))
                        await package.SetPngAsTranslucentJpegAsync(saveThumbnail4Key, chroniclePropertySet.Thumbnail).ConfigureAwait(false);
                enhancedPackageMemoryStream = new MemoryStream();
                await package.CopyToAsync(enhancedPackageMemoryStream).ConfigureAwait(false);
                enhancedPackageMemoryStream.Seek(0, SeekOrigin.Begin);
                using var sha256 = SHA256.Create();
                fileHashArray = await sha256.ComputeHashAsync(enhancedPackageMemoryStream).ConfigureAwait(false);
                if (await chronicleDbContext.KnownSavePackageHashes.FirstOrDefaultAsync(esp => esp.Sha256 == fileHashArray).ConfigureAwait(false) is not { } knownSavePackageHash)
                {
                    knownSavePackageHash = new KnownSavePackageHash { Sha256 = fileHashArray };
                    await chronicleDbContext.KnownSavePackageHashes.AddAsync(knownSavePackageHash).ConfigureAwait(false);
                }
                newSnapshot.EnhancedSavePackageHash = knownSavePackageHash;
                enhancedPackageMemoryStream.Seek(0, SeekOrigin.Begin);
            }
            await package.DisposeAsync().ConfigureAwait(false);
            DiagnosticStatus = $"{(isSingleEnqueuedFile ? "Single" : "Batch")}: {fileInfo.Name} / Commit";
            await chronicleDbContext.SavePackageSnapshots.AddAsync(newSnapshot).ConfigureAwait(false);
            await chronicleDbContext.SaveChangesAsync().ConfigureAwait(false);
            if (enhancedPackageMemoryStream is not null)
            {
                using var savePackageStream = new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                await enhancedPackageMemoryStream.CopyToAsync(savePackageStream).ConfigureAwait(false);
                await savePackageStream.FlushAsync().ConfigureAwait(false);
                savePackageStream.Close();
                await enhancedPackageMemoryStream.DisposeAsync().ConfigureAwait(false);
            }
            using var chroniclesLockHeld = await chroniclesLock.LockAsync().ConfigureAwait(false);
            if (!chronicleByNucleusIdAndCreated.TryGetValue((account.NucleusId, account.Created), out var chronicle))
            {
                chronicle = new(loggerFactory, loggerFactory.CreateLogger<Chronicle>(), chronicleDbContextFactory, this);
                chronicleByNucleusIdAndCreated.Add((account.NucleusId, account.Created), chronicle);
                await chronicle.FirstLoadComplete.ConfigureAwait(false);
                chronicles.Add(chronicle);
            }
            else if (newSnapshot is not null)
            {
                await chronicle.ReloadScalarsAsync().ConfigureAwait(false);
                await chronicle.LoadSnapshotAsync(newSnapshot).ConfigureAwait(false);
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
            logger.LogWarning(ex, "unexpected exception encountered while processing {FilePath}", fileInfo.FullName);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(AppText.Archivist_Warning_CannotReadSaveFile, fileInfo.FullName, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
            {
                options.RequireInteraction = true;
                options.Icon = MaterialDesignIcons.Normal.ContentSaveAlert;
            });
        }
        finally
        {
            if (package is not null)
                await package.DisposeAsync().ConfigureAwait(false);
        }
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task ProcessPathsQueueAsync()
    {
        if (this.pathsProcessingQueue is not { } pathsProcessingQueue)
            return;
        while (await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var nomNom = new Queue<string>();
            var alreadyNom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            nomNom.Enqueue(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
            while (true)
            {
                try
                {
                    if (!await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                        break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                try
                {
                    while (await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                    {
                        var path = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                        if (!alreadyNom.Contains(path))
                        {
                            nomNom.Enqueue(path);
                            alreadyNom.Add(path);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            try
            {
                if (settings.ArchivistEnabled)
                {
                    State = ArchivistState.Ingesting;
                    var gameWasRunningAtStart = await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false) is not null;
                    try
                    {
                        var savesDirectoryPath = Path.GetFullPath(Path.Combine(settings.UserDataFolderPath, "saves"));
                        while (settings.ArchivistEnabled && nomNom.TryDequeue(out var path))
                        {
                            FileSystemInfo? fileSystemInfo = File.Exists(path)
                                ? new FileInfo(path)
                                : Directory.Exists(path)
                                ? new DirectoryInfo(path)
                                : null;
                            if (fileSystemInfo is null)
                                continue;
                            var isInSavesDirectory = savesDirectoryPath == Path.GetFullPath(fileSystemInfo is FileInfo fileInfo
                                ? fileInfo.Directory!.FullName
                                : fileSystemInfo is DirectoryInfo directoryInfo
                                ? directoryInfo.FullName
                                : string.Empty);
                            if (isInSavesDirectory
                                && !settings.ArchivistAutoIngestSaves)
                                continue;
                            if (isInSavesDirectory
                                && modsDirectoryCataloger.State is not (ModsDirectoryCatalogerState.Idle or ModsDirectoryCatalogerState.Sleeping))
                            {
                                State = ArchivistState.AwaitingModCataloging;
                                await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
                                State = ArchivistState.Ingesting;
                            }
                            var savesDirectoryInfo = new DirectoryInfo(savesDirectoryPath);
                            var singleSaveFile = new FileInfo(path);
                            if (singleSaveFile.Exists
                                && extensions.Contains(singleSaveFile.Extension)
                                && (!isInSavesDirectory || GetSavesDirectoryLegalFilenamePattern().IsMatch(singleSaveFile.Name)))
                            {
                                DiagnosticStatus = $"Single: {singleSaveFile.Name}";
                                await ProcessDequeuedFileAsync(new FileInfo(path), isInSavesDirectory, true, gameWasRunningAtStart).ConfigureAwait(false);
                            }
                            else if (Directory.Exists(path))
                                foreach (var directoryFileInfo in new DirectoryInfo(path)
                                    .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                    .Where(file => extensions.Contains(file.Extension) && (!isInSavesDirectory || GetSavesDirectoryLegalFilenamePattern().IsMatch(file.Name)))
                                    .OrderBy(file => file.LastWriteTime)
                                    .ToImmutableArray())
                                {
                                    DiagnosticStatus = $"Batch: {directoryFileInfo.Name}";
                                    await ProcessDequeuedFileAsync(directoryFileInfo, isInSavesDirectory, false, gameWasRunningAtStart).ConfigureAwait(false);
                                    if (!settings.ArchivistEnabled
                                        || isInSavesDirectory
                                        && !settings.ArchivistAutoIngestSaves)
                                        break;
                                }
                        }
                    }
                    finally
                    {
                        State = ArchivistState.Idle;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unhandled exception while processing the paths queue");
            }
            finally
            {
            }
        }
    }

    public async Task ReapplyEnhancementsAsync(Chronicle chronicle)
    {
        ArgumentNullException.ThrowIfNull(chronicle);
        var savesDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"));
        if (!savesDirectory.Exists)
            return;
        foreach (var saveFile in savesDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(file => GetSavesDirectoryLegalFilenamePattern().IsMatch(file.Name))
            .OrderBy(file => file.LastWriteTime)
            .ToImmutableArray())
        {
            var saveFileHash = await ModFileManifestModel.GetFileSha256HashAsync(saveFile.FullName).ConfigureAwait(false);
            if (chronicle.Snapshots.FirstOrDefault(s => s.OriginalPackageSha256.SequenceEqual(saveFileHash)
                || s.EnhancedPackageSha256.SequenceEqual(saveFileHash)) is not { } snapshot)
                continue;
            string tempFileName;
            using (var package = await DataBasePackedFile.FromPathAsync(saveFile.FullName).ConfigureAwait(false))
            {
                var packageKeys = await package.GetKeysAsync().ConfigureAwait(false);
                var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                if (saveGameDataKeys.Length is <= 0 or >= 2)
                    continue;
                var saveGameDataKey = saveGameDataKeys[0];
                IDbContextFactory<ChronicleDbContext> chronicleDbContextFactory = new ChronicleDbContextFactory(new FileInfo(Path.Combine(settings.ArchiveFolderPath, $"N-{chronicle.NucleusId:x16}-C-{chronicle.Created:x16}.chronicle.sqlite")));
                using var chronicleDbContext = await chronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if ((await chronicleDbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                    await chronicleDbContext.Database.MigrateAsync().ConfigureAwait(false);
                if (await chronicleDbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                    continue;
                if (await chronicleDbContext.SavePackageSnapshots.Include(sps => sps.EnhancedSavePackageHash).FirstOrDefaultAsync(sps => sps.Id == snapshot.SavePackageSnapshotId).ConfigureAwait(false) is not { } savePackageSnapshot)
                    continue;
                if (!string.IsNullOrWhiteSpace(chronicle.GameNameOverride))
                {
                    var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await package.GetAsync(saveGameDataKey).ConfigureAwait(false));
                    if (chronicle.GameNameOverride?.Trim() is { Length: > 0 } gameNameOverride
                        && saveGameData.SaveSlot is { } saveSlot
                        && saveSlot.SlotName != gameNameOverride)
                    {
                        saveSlot.SlotName = chronicle.GameNameOverride;
                        await package.SetAsync(saveGameDataKey, saveGameData.ToProtobufMessage(), package.GetExplicitCompressionMode(saveGameDataKey)).ConfigureAwait(false);
                    }
                }
                byte[]? thumbnailBytes = null;
                if (propertySet.Thumbnail is { Length: > 0 } customThumbnailBytes)
                    thumbnailBytes = customThumbnailBytes;
                else if (savePackageSnapshot.Thumbnail is { Length: > 0 } originalThumbnailBytes)
                    thumbnailBytes = originalThumbnailBytes;
                if (thumbnailBytes is { Length: > 0 })
                    foreach (var saveThumbnail4Key in packageKeys.Where(key => key.Type is ResourceType.SaveGameHouseholdThumbnail))
                        await package.SetPngAsTranslucentJpegAsync(saveThumbnail4Key, thumbnailBytes).ConfigureAwait(false);
                tempFileName = $"{saveFile.FullName}.tmp";
                await package.SaveAsAsync(tempFileName).ConfigureAwait(false);
                var newEnhancedHashBytes = (await ModFileManifestModel.GetFileSha256HashAsync(tempFileName).ConfigureAwait(false)).ToArray();
                if (await chronicleDbContext.KnownSavePackageHashes.FirstOrDefaultAsync(ksph => ksph.Sha256 == newEnhancedHashBytes).ConfigureAwait(false) is not { } knownSavePackageHash)
                {
                    knownSavePackageHash = new KnownSavePackageHash { Sha256 = newEnhancedHashBytes };
                    await chronicleDbContext.KnownSavePackageHashes.AddAsync(knownSavePackageHash).ConfigureAwait(false);
                }
                savePackageSnapshot.EnhancedSavePackageHash = knownSavePackageHash;
                await chronicleDbContext.SaveChangesAsync().ConfigureAwait(false);
                await snapshot.ReloadScalarsAsync(savePackageSnapshot).ConfigureAwait(false);
            }
            File.Move(tempFileName, saveFile.FullName, overwrite: true);
        }
    }

    void ReconnectFolders() =>
        Task.Run(ReconnectFoldersAsync);

    async Task ReconnectFoldersAsync()
    {
        await DisconnectFromFoldersAsync().ConfigureAwait(false);
        await ConnectToFoldersAsync().ConfigureAwait(false);
    }

    async Task RouteCriticalExceptionAsync(Exception ex)
    {
        settings.ArchivistEnabled = false;
        logger.LogError(ex, "connecting to the archives folder at this path failed: {ArchivesFolderPath}", settings.ArchiveFolderPath);
        superSnacks.OfferRefreshments
        (
            new MarkupString(string.Format(AppText.Archivist_Warning_CannotConnectToArchivesFolder, settings.ArchiveFolderPath, ex.GetType().Name, ex.Message)),
            Severity.Error,
            options =>
            {
                options.Icon = MaterialDesignIcons.Normal.ArchiveAlert;
                options.RequireInteraction = true;
            }
        );
        if (!appLifecycleManager.IsVisible)
            await platformFunctions.SendLocalNotificationAsync(AppText.Archivist_Notification_CannotConnectToArchivesFolder_Caption, AppText.Archivist_Notification_CannotConnectToArchivesFolder_Test).ConfigureAwait(false);
    }
}
