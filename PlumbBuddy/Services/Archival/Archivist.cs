using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services.Archival;

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
    public static partial Regex GetSavesDirectoryLegalFilenamePattern();

    public Archivist(ILoggerFactory loggerFactory, ILogger<Archivist> logger, IDbContextFactory<PbDbContext> pbDbContextFactory, IPlatformFunctions platformFunctions, IAppLifecycleManager appLifecycleManager, ISettings settings, IModsDirectoryCataloger modsDirectoryCataloger, IProxyHost proxyHost, ISuperSnacks superSnacks, IUserInterfaceMessaging userInterfaceMessaging)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modsDirectoryCataloger);
        ArgumentNullException.ThrowIfNull(proxyHost);
        ArgumentNullException.ThrowIfNull(superSnacks);
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        this.loggerFactory = loggerFactory;
        this.logger = logger;
        this.pbDbContextFactory = pbDbContextFactory;
        this.platformFunctions = platformFunctions;
        this.appLifecycleManager = appLifecycleManager;
        this.settings = settings;
        this.modsDirectoryCataloger = modsDirectoryCataloger;
        this.proxyHost = proxyHost;
        this.superSnacks = superSnacks;
        this.userInterfaceMessaging = userInterfaceMessaging;
        chronicleByNucleusIdAndCreated = [];
        chronicles = [];
        Chronicles = new(chronicles);
        chroniclesLock = new();
        connectionLock = new();
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        if (this.settings.ArchivistEnabled)
            ConnectToFolders();
        WarnIfDisabled();
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
    AsyncProducerConsumerQueue<(string path, bool manuallyAdded)>? pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly IProxyHost proxyHost;
    bool savesFolderScanned;
    string savesFolderPath = string.Empty;
    Chronicle? selectedChronicle;
    readonly ISettings settings;
    string snapshotsSearchText = string.Empty;
    ArchivistState state;
    readonly ISuperSnacks superSnacks;
    readonly IUserInterfaceMessaging userInterfaceMessaging;

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
            : pathsProcessingQueue.EnqueueAsync((fileSystemInfo.FullName, true));
    }

    void ConnectToFolders() =>
        Task.Run(ConnectToFoldersAsync);

    async Task ConnectToFoldersAsync()
    {
        using var connectionLockHeld = await connectionLock.LockAsync().ConfigureAwait(false);
        var userDataFolder = new DirectoryInfo(settings.UserDataFolderPath);
        if (!settings.ArchivistEnabled
            || fileSystemWatcher is not null
            || !userDataFolder.Exists)
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
        fileSystemWatcher = new FileSystemWatcher(userDataFolder.FullName)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024,
            NotifyFilter =
                  NotifyFilters.CreationTime
                | NotifyFilters.DirectoryName
                | NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
        };
        fileSystemWatcher.Changed += HandleFileSystemWatcherChanged;
        fileSystemWatcher.Created += HandleFileSystemWatcherCreated;
        fileSystemWatcher.Deleted += HandleFileSystemWatcherDeleted;
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
        {
            savesFolderPath = Path.GetFullPath(Path.Combine(userDataFolder.FullName, "saves"));
            if (Directory.Exists(savesFolderPath))
                await pathsProcessingQueue.EnqueueAsync((savesFolderPath, false)).ConfigureAwait(false);
        }
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
            fileSystemWatcher.Deleted -= HandleFileSystemWatcherDeleted;
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

    void HandleFileSystemWatcherChanged(object sender, FileSystemEventArgs e) =>
        pathsProcessingQueue?.Enqueue((e.FullPath, false));

    void HandleFileSystemWatcherCreated(object sender, FileSystemEventArgs e) =>
        pathsProcessingQueue?.Enqueue((e.FullPath, false));

    void HandleFileSystemWatcherDeleted(object sender, FileSystemEventArgs e)
    {
        if (Path.GetFullPath(e.FullPath) == savesFolderPath)
            savesFolderScanned = false;
    }

    void HandleFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError(e.GetException(), "saves directory monitoring encountered unexpected unhandled exception");
        ReconnectFolders();
    }

    void HandleFileSystemWatcherRenamed(object sender, RenamedEventArgs e)
    {
        if (Path.GetFullPath(e.OldFullPath) == savesFolderPath)
            savesFolderScanned = false;
        pathsProcessingQueue?.Enqueue((e.FullPath, false));
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ArchiveFolderPath)
            or nameof(ISettings.UserDataFolderPath))
            ReconnectFolders();
        else if (e.PropertyName is nameof(ISettings.ArchivistAutoIngestSaves))
        {
            if (settings.ArchivistAutoIngestSaves)
                pathsProcessingQueue?.Enqueue((Path.Combine(settings.UserDataFolderPath, "saves"), false));
        }
        else if (e.PropertyName is nameof(ISettings.ArchivistEnabled))
        {
            if (settings.ArchivistEnabled)
                ConnectToFolders();
            else
                DisconnectFromFolders();
        }
        else if (e.PropertyName is nameof(ISettings.Onboarded))
            WarnIfDisabled();
        else if (e.PropertyName is nameof(ISettings.UserDataFolderPath))
        {
            if (settings.ArchivistEnabled)
                ReconnectFolders();
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
    [SuppressMessage("Maintainability", "CA1505: Avoid unmaintainable code")]
    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    async Task ProcessDequeuedFileAsync(FileInfo fileInfo, bool isInSavesDirectory, DateTime? gameStarted)
    {
        DataBasePackedFile? package = null;
        try
        {
            var wasLive = false;
            ImmutableArray<byte> fileHash;
            while (true)
            {
                DiagnosticStatus = $"{fileInfo.Name} / Wait";
                fileInfo.Refresh();
                var length = fileInfo.Length;
                while (gameStarted is not null
                    && fileInfo.LastWriteTime > gameStarted)
                {
                    await Task.Delay(oneQuarterSecond).ConfigureAwait(false);
                    fileInfo.Refresh();
                    var newLength = fileInfo.Length;
                    if (length == newLength)
                        break;
                    length = newLength;
                }
                wasLive = isInSavesDirectory
                    && gameStarted is not null
                    && fileInfo.LastWriteTime > gameStarted;
                DiagnosticStatus = $"{fileInfo.Name} / SHA256 Package";
                try
                {
                    fileHash = await ModFileManifestModel.GetFileSha256HashAsync(fileInfo.FullName).ConfigureAwait(false);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(oneQuarterSecond).ConfigureAwait(false);
                }
            }
            using (var chroniclesLockHeldForLoadCheck = await chroniclesLock.LockAsync().ConfigureAwait(false))
                if (chronicles.Any(c => c.Snapshots.Any(s => s.OriginalPackageSha256.SequenceEqual(fileHash) || s.EnhancedPackageSha256.SequenceEqual(fileHash))))
                    return;
            var fileHashArray = Unsafe.As<ImmutableArray<byte>, byte[]>(ref fileHash);
            package = await DataBasePackedFile.FromPathAsync(fileInfo.FullName).ConfigureAwait(false);
            var packageKeys = await package.GetKeysAsync().ConfigureAwait(false);
            var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
            if (saveGameDataKeys.Length is <= 0 or >= 2)
                throw new FormatException("save package contains invalid number of save game data resources");
            var saveGameDataKey = saveGameDataKeys[0];
            DiagnosticStatus = $"{fileInfo.Name} / Deserialize SGD";
            var saveGameData = Serializer.Deserialize<SaveGameData>(await package.GetAsync(saveGameDataKey).ConfigureAwait(false));
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
                Memory<byte> nucleusIdBytesMemory = nucleusIdBytes;
                MemoryMarshal.Write(nucleusIdBytesMemory.Span, in nucleusId);
                var created = account.Created;
                var createdBytes = new byte[8];
                Memory<byte> createdBytesMemory = createdBytes;
                MemoryMarshal.Write(createdBytesMemory.Span, in created);
                chroniclePropertySet = new ChroniclePropertySet
                {
                    Created = createdBytes,
                    Name = saveSlot.SlotName,
                    NucleusId = nucleusIdBytes,
                };
                await chronicleDbContext.ChroniclePropertySets.AddAsync(chroniclePropertySet).ConfigureAwait(false);
            }
            DiagnosticStatus = $"{fileInfo.Name} / Last Snapshot";
            var lastSnapshot = await chronicleDbContext.SavePackageSnapshots
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            var activeHousehold = saveGameData.Households.FirstOrDefault(h => h.HouseholdId == saveSlot.ActiveHouseholdId);
            var zoneId = saveSlot.GameplayData?.CameraData?.ZoneId ?? default;
            var lastZone = saveGameData.Zones.FirstOrDefault(z => z.ZoneId == zoneId);
            var neighborhoodId = lastZone?.NeighborhoodId ?? default;
            var lastNeighborhood = saveGameData.Neighborhoods.FirstOrDefault(n => n.NeighborhoodId == neighborhoodId);
            await chronicleDbContext.Database.ExecuteSqlRawAsync("INSERT INTO KnownSavePackageHashes (Sha256) VALUES ({0}) ON CONFLICT DO NOTHING", fileHashArray).ConfigureAwait(false);
            newSnapshot = new SavePackageSnapshot
            {
                ActiveHouseholdName = activeHousehold?.Name,
                LastPlayedLotName = lastZone?.Name,
                LastPlayedWorldName = lastNeighborhood?.Name,
                LastWriteTime = fileInfo.LastWriteTime,
                Label = $"Snapshot {(lastSnapshot is null ? 0 : lastSnapshot.Id) + 1:n0}",
                OriginalSavePackageHash = await chronicleDbContext.KnownSavePackageHashes.FirstAsync(mfh => mfh.Sha256 == fileHashArray).ConfigureAwait(false)
            };
            DiagnosticStatus = $"{fileInfo.Name} / Indexing Relationships";
            var indexedRelationships = saveSlot.GameplayData is { } gameplayData
                && gameplayData.RelationshipService is { } relationshipService
                && relationshipService.Relationships is { Count: > 0 } relationships
                ? relationships.ToImmutableDictionary(r => new RelationshipKey(r.SimIdA, r.SimIdB))
                : ImmutableDictionary<RelationshipKey, PersistableServiceRelationship>.Empty;
            DiagnosticStatus = $"{fileInfo.Name} / Checking for Siblings With Romantic Relationships";
            var sims = saveGameData.Sims.ToImmutableDictionary(sd => sd.SimId);
            var households = saveGameData.Households.ToImmutableDictionary(hd => hd.HouseholdId);
            var zones = saveGameData.Zones.ToImmutableDictionary(zd => zd.ZoneId);
            var neighborhoods = saveGameData.Neighborhoods.ToImmutableDictionary(nd => nd.NeighborhoodId);
            foreach (var (mom, children) in saveGameData.Sims
                .SelectMany
                (
                    sd => sd.Attributes
                        ?.GenealogyTracker
                        ?.FamilyRelations
                        .Where(fr => fr.RelationType is RelationshipIndex.RelationshipMother)
                        .Select(fr => (mother: fr.SimId, child: sd.SimId))
                        ?? Enumerable.Empty<(ulong mother, ulong child)>()
                )
                .GroupBy(motherhood => motherhood.mother)
                .Where(g => g.Count() > 1)
                .Select(g => (mom: g.Key, children: g.Select(motherhood => motherhood.child).ToImmutableArray())))
            {
                for (var siblingAIndex = 0; siblingAIndex < children.Length - 1; ++siblingAIndex)
                    for (var siblingBIndex = siblingAIndex + 1; siblingBIndex < children.Length; ++siblingBIndex)
                    {
                        var siblingAId = children[siblingAIndex];
                        var siblingBId = children[siblingBIndex];
                        if (indexedRelationships.TryGetValue(new RelationshipKey(siblingAId, siblingBId), out var relationship)
                            && relationship.BidirectionalRelationshipData is { } bidirectional
                            && bidirectional.Tracks.Any(track => track.TrackId is 0x410B && track.Value != 0))
                        {
                            var siblingA = sims.TryGetValue(siblingAId, out var siblingAValue) ? siblingAValue : null;
                            var siblingAHousehold = households.TryGetValue(siblingA?.HouseholdId ?? 0, out var siblingAHouseholdValue) ? siblingAHouseholdValue : null;
                            var siblingAHomeZone = zones.TryGetValue(siblingAHousehold?.HomeZone ?? 0, out var siblingAHomeZoneValue) ? siblingAHomeZoneValue : null;
                            var siblingAHomeNeighborhood = neighborhoods.TryGetValue(siblingAHomeZone?.NeighborhoodId ?? 0, out var siblingAHomeNeighborhoodValue) ? siblingAHomeNeighborhoodValue : null;
                            var siblingB = sims.TryGetValue(siblingBId, out var siblingBValue) ? siblingBValue : null;
                            var siblingBHousehold = households.TryGetValue(siblingB?.HouseholdId ?? 0, out var siblingBHouseholdValue) ? siblingBHouseholdValue : null;
                            var siblingBHomeZone = zones.TryGetValue(siblingBHousehold?.HomeZone ?? 0, out var siblingBHomeZoneValue) ? siblingBHomeZoneValue : null;
                            var siblingBHomeNeighborhood = neighborhoods.TryGetValue(siblingBHomeZone?.NeighborhoodId ?? 0, out var siblingBHomeNeighborhoodValue) ? siblingBHomeNeighborhoodValue : null;
                            newSnapshot.Defects.Add
                            (
                                new
                                (
                                    newSnapshot,
                                    string.Format
                                    (
                                        AppText.Archivist_SnapshotDefect_SiblingsWithRomanticRelationship_Description,
                                        siblingA?.FirstName ?? "?",
                                        siblingA?.LastName ?? "?",
                                        siblingAId,
                                        siblingAHousehold?.Name ?? "?",
                                        siblingAHomeZone?.Name ?? "?",
                                        siblingAHomeNeighborhood?.Name ?? "?",
                                        siblingB?.FirstName ?? "?",
                                        siblingB?.LastName ?? "?",
                                        siblingBId,
                                        siblingBHousehold?.Name ?? "?",
                                        siblingBHomeZone?.Name ?? "?",
                                        siblingBHomeNeighborhood?.Name ?? "?"
                                    )
                                )
                                {
                                    SavePackageSnapshotDefectType = SavePackageSnapshotDefectType.SiblingsWithRomanticRelationship
                                }
                            );
                        }
                    }
            }
            var contextLock = new AsyncLock();
            using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4)))
                await Task.WhenAll(packageKeys.Select(async key =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var keyBytes = new byte[16];
                        Memory<byte> keyBytesMemory = keyBytes;
                        var type = key.Type;
                        MemoryMarshal.Write(keyBytesMemory.Span[0..4], in type);
                        var group = key.Group;
                        MemoryMarshal.Write(keyBytesMemory.Span[4..8], in group);
                        var fullInstance = key.FullInstance;
                        MemoryMarshal.Write(keyBytesMemory.Span[8..16], in fullInstance);
                        var explicitCompressionMode = package.GetExplicitCompressionMode(key);
                        var content =
                              explicitCompressionMode is LlamaLogic.Packages.CompressionMode.CallerSuppliedStreamable
                            ? await package.GetRawAsync(key).ConfigureAwait(false)
                            : await package.GetAsync(key, true).ConfigureAwait(false);
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
                                var png = await package.GetTranslucentJpegAsPngAsync(key, true).ConfigureAwait(false);
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
                            DiagnosticStatus = $"{fileInfo.Name} / Load Prev {key}";
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
                            DiagnosticStatus = $"{fileInfo.Name} / Delta Gen {key}";
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
                                DiagnosticStatus = $"{fileInfo.Name} / Comp Delta {key}";
                                var patchZlib = (await DataBasePackedFile.ZLibCompressAsync(patch).ConfigureAwait(false)).ToArray();
                                using var heldContextLockForAddDelta = await contextLock.LockAsync().ConfigureAwait(false);
                                await chronicleDbContext.ResourceSnapshotDeltas.AddAsync
                                (
                                    new ResourceSnapshotDelta(newSnapshot, resource)
                                    {
                                        PatchZLib = patchZlib,
                                        PatchSize = patch.Length,
                                        CompressionType = previousCompressionType
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
                        newSnapshot.Resources.Add(resource);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })).ConfigureAwait(false);
            if (wasLive)
            {
                DiagnosticStatus = $"{fileInfo.Name} / Live";
                newSnapshot.WasLive = true;
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await foreach (var modFile in pbDbContext.ModFiles
                    .Where(mf => mf.FoundAbsent == null && (mf.FileType == ModsDirectoryFileType.Package || mf.FileType == ModsDirectoryFileType.ScriptArchive))
                    .Include(mf => mf.ModFileHash)
                    .AsAsyncEnumerable())
                {
                    var modFileLastWrite = modFile.LastWrite;
                    var modFilePath = modFile.Path;
                    var modFileSha256 = modFile.ModFileHash.Sha256;
                    var modFileSize = modFile.Size;
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
                    snapshotModFile.Snapshots.Add(newSnapshot);
                }
            }
            MemoryStream? enhancedPackageMemoryStream = null;
            ReadOnlyMemory<byte> customThumbnail = chroniclePropertySet.Thumbnail;
            if (isInSavesDirectory
                && (!string.IsNullOrWhiteSpace(chroniclePropertySet.GameNameOverride)
                || !customThumbnail.IsEmpty))
            {
                DiagnosticStatus = $"{fileInfo.Name} / Customize";
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
                await chronicleDbContext.Database.ExecuteSqlRawAsync("INSERT INTO KnownSavePackageHashes (Sha256) VALUES ({0}) ON CONFLICT DO NOTHING", fileHashArray).ConfigureAwait(false);
                newSnapshot.EnhancedSavePackageHash = await chronicleDbContext.KnownSavePackageHashes.FirstAsync(esp => esp.Sha256 == fileHashArray).ConfigureAwait(false);
                enhancedPackageMemoryStream.Seek(0, SeekOrigin.Begin);
            }
            await package.DisposeAsync().ConfigureAwait(false);
            DiagnosticStatus = $"{fileInfo.Name} / Commit";
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
            Snapshot? viewSnapshot = null;
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
                viewSnapshot = await chronicle.LoadSnapshotAsync(newSnapshot).ConfigureAwait(false);
            }
            var disabledSavePackageSnapshotDefectTypes = (await chronicleDbContext.DisabledSavePackageSnapshotDefectTypes.Select(dspsdt => dspsdt.SavePackageSnapshotDefectType).ToListAsync().ConfigureAwait(false)).ToImmutableHashSet();
            if (newSnapshot is not null
                && newSnapshot.Defects.Any(d => !disabledSavePackageSnapshotDefectTypes.Contains(d.SavePackageSnapshotDefectType)))
            {
                superSnacks.OfferRefreshments(new MarkupString(AppText.Archivist_SnapshotDefect_Snack), Severity.Warning, options =>
                {
                    options.Icon = MaterialDesignIcons.Normal.ContentSaveAlert;
                    options.OnClick = async _ =>
                    {
                        if (chronicles.Contains(chronicle))
                            SelectedChronicle = chronicle;
                        if (viewSnapshot is not null)
                            viewSnapshot.ShowDetails = true;
                        userInterfaceMessaging.ShowArchivist();
                    };
                    options.RequireInteraction = true;
                });
                await proxyHost.ForegroundPlumbBuddyAsync(pauseGame: true).ConfigureAwait(false);
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
                options.Icon = MaterialDesignIcons.Normal.ContentSaveAlert;
                options.RequireInteraction = true;
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
            var list = new List<(string path, bool manuallyAdded)>();
            var alreadyNomed = new Dictionary<string, bool>();
            list.Add(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
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
                        var (path, manuallyAdded) = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                        if (!alreadyNomed.TryGetValue(path, out var alreadyManuallyAdded))
                        {
                            list.Add((path, manuallyAdded));
                            continue;
                        }
                        if (manuallyAdded && !alreadyManuallyAdded)
                            list[list.FindIndex(t => t.path == path)] = (path, true);
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            var nomNom = new Queue<(string path, bool manuallyAdded)>(list);
            try
            {
                if (settings.ArchivistEnabled)
                {
                    State = ArchivistState.Ingesting;
                    var gameStarted = (await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false))?.StartTime;
                    try
                    {
                        var savesDirectoryPath = Path.GetFullPath(Path.Combine(settings.UserDataFolderPath, "saves"));
                        while (settings.ArchivistEnabled && nomNom.TryDequeue(out var tuple))
                        {
                            var (path, manuallyAdded) = tuple;
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
                            if (isInSavesDirectory)
                            {
                                if (!settings.ArchivistAutoIngestSaves)
                                    continue;
                                if (modsDirectoryCataloger.State is not (ModsDirectoryCatalogerState.Idle or ModsDirectoryCatalogerState.Sleeping))
                                {
                                    State = ArchivistState.AwaitingModCataloging;
                                    await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
                                    State = ArchivistState.Ingesting;
                                }
                            }
                            else if (!manuallyAdded)
                                continue;
                            var savesDirectoryInfo = new DirectoryInfo(savesDirectoryPath);
                            var singleSaveFile = new FileInfo(path);
                            if (singleSaveFile.Exists
                                && extensions.Contains(singleSaveFile.Extension)
                                && (!isInSavesDirectory || GetSavesDirectoryLegalFilenamePattern().IsMatch(singleSaveFile.Name)))
                            {
                                DiagnosticStatus = "Waiting for TS4 to Finish Saving";
                                await proxyHost.WaitForSavesAccessAsync().ConfigureAwait(false);
                                DiagnosticStatus = $"Single: {singleSaveFile.Name}";
                                await ProcessDequeuedFileAsync(new FileInfo(path), isInSavesDirectory, gameStarted).ConfigureAwait(false);
                            }
                            else if (Directory.Exists(path))
                            {
                                if (isInSavesDirectory)
                                {
                                    if (savesFolderScanned)
                                        continue;
                                    savesFolderScanned = true;
                                }
                                foreach (var directoryFileInfo in new DirectoryInfo(path)
                                    .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                    .Where(file => extensions.Contains(file.Extension) && (!isInSavesDirectory || GetSavesDirectoryLegalFilenamePattern().IsMatch(file.Name)))
                                    .OrderBy(file => file.LastWriteTime)
                                    .ToImmutableArray())
                                {
                                    DiagnosticStatus = "Waiting for TS4 to Finish Saving";
                                    await proxyHost.WaitForSavesAccessAsync().ConfigureAwait(false);
                                    DiagnosticStatus = $"Batch: {directoryFileInfo.Name}";
                                    await ProcessDequeuedFileAsync(directoryFileInfo, isInSavesDirectory, gameStarted).ConfigureAwait(false);
                                    if (!settings.ArchivistEnabled
                                        || isInSavesDirectory
                                        && !settings.ArchivistAutoIngestSaves)
                                        break;
                                }
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
                    var saveGameData = Serializer.Deserialize<SaveGameData>(await package.GetAsync(saveGameDataKey).ConfigureAwait(false));
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
                await chronicleDbContext.Database.ExecuteSqlRawAsync("INSERT INTO KnownSavePackageHashes (Sha256) VALUES ({0}) ON CONFLICT DO NOTHING", newEnhancedHashBytes).ConfigureAwait(false);
                savePackageSnapshot.EnhancedSavePackageHash = await chronicleDbContext.KnownSavePackageHashes.FirstAsync(ksph => ksph.Sha256 == newEnhancedHashBytes).ConfigureAwait(false);
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

    void WarnIfDisabled() =>
        _ = Task.Run(WarnIfDisabledAsync);

    async Task WarnIfDisabledAsync()
    {
        if (!settings.Onboarded || settings.ArchivistEnabled && settings.ArchivistAutoIngestSaves)
            return;
        await Task.Delay(500).ConfigureAwait(false);
        await modsDirectoryCataloger.WaitForIdleAsync().ConfigureAwait(false);
        superSnacks.OfferRefreshments(new MarkupString(AppText.Archivist_Disabled_Snack), Severity.Info, options =>
        {
            options.Icon = MaterialDesignIcons.Normal.ContentSaveCheck;
            options.OnClick = async _ => userInterfaceMessaging.ShowArchivist();
        });
    }
}
