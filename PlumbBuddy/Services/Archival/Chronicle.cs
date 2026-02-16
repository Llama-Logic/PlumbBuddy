using Epiforge.Extensions.Collections.Specialized;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using ResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;
using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services.Archival;

[SuppressMessage("Naming", "CA1724: Type names should not match namespaces")]
public sealed partial class Chronicle :
    PropertyChangeNotifier
{
    public Chronicle(ILoggerFactory loggerFactory, ILogger<Chronicle> logger, IDbContextFactory<ChronicleDbContext> dbContextFactory, IMainThreadDetails mainThreadDetails, IArchivist archivist)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(mainThreadDetails);
        ArgumentNullException.ThrowIfNull(archivist);
        this.loggerFactory = loggerFactory;
        this.logger = logger;
        this.dbContextFactory = dbContextFactory;
        this.mainThreadDetails = mainThreadDetails;
        this.archivist = archivist;
        loadScrapbookImagesCancellationTokenSourceLock = new();
        scrapbookImages = [];
        collectionObserver = new CollectionObserver();
        var listQuery = collectionObserver.ObserveReadOnlyList(scrapbookImages);
        scrapbookImagesQuery = listQuery.DisposeWhenDependentDisposed(() => listQuery.ObserveUsingSynchronizationContextEventually(mainThreadDetails.SynchronizationContext));
        snapshots = [];
        Snapshots = new(snapshots);
        firstLoadComplete = new();
        loadScrapbookImagesLock = new();
        _ = Task.Run(LoadAllAsync);
    }

    readonly IArchivist archivist;
    ulong basisCreated;
    ulong basisNucleusId;
    ImmutableArray<byte> basisOriginalPackageSha256 = [];
    readonly ICollectionObserver collectionObserver;
    ulong created;
    long databaseSize;
    readonly IDbContextFactory<ChronicleDbContext> dbContextFactory;
    DateTimeOffset earliestLastWriteTime;
    readonly TaskCompletionSource firstLoadComplete;
    string? gameNameOverride;
    bool isLoadingScrapbookImages;
    DateTimeOffset latestLastWriteTime;
    CancellationTokenSource? loadScrapbookImagesCancellationTokenSource;
    readonly AsyncLock loadScrapbookImagesCancellationTokenSourceLock;
    readonly AsyncLock loadScrapbookImagesLock;
    readonly ILogger<Chronicle> logger;
    readonly ILoggerFactory loggerFactory;
    readonly IMainThreadDetails mainThreadDetails;
    string name = string.Empty;
    string? notes;
    ulong nucleusId;
    readonly ObservableCollection<ScrapbookImage> scrapbookImages;
    readonly IObservableCollectionQuery<ScrapbookImage> scrapbookImagesQuery;
    readonly ObservableCollection<Snapshot> snapshots;
    ImmutableArray<byte> thumbnail = [];
    IReadOnlyCollection<SavePackageSnapshotDefectType>? watchedSnapshotDefects;

    public IArchivist Archivist =>
        archivist;

    public Snapshot? BasedOnSnapshot =>
        archivist.Chronicles.FirstOrDefault(c => c.NucleusId == basisNucleusId && c.Created == basisCreated) is { } basisChronicle
        && basisChronicle.Snapshots.FirstOrDefault(s => s.OriginalPackageSha256.SequenceEqual(basisOriginalPackageSha256)) is { } basisSnapshot
        ? basisSnapshot
        : null;

    public ulong BasisCreated =>
        basisCreated;

    public ulong BasisNucleusId =>
        basisNucleusId;

    public ulong Created =>
        created;

    public long DatabaseSize
    {
        get => databaseSize;
        private set
        {
            if (databaseSize == value)
                return;
            databaseSize = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset EarliestLastWriteTime
    {
        get => earliestLastWriteTime;
        private set
        {
            if (earliestLastWriteTime == value)
                return;
            earliestLastWriteTime = value;
            OnPropertyChanged();
        }
    }

    public Task FirstLoadComplete =>
        firstLoadComplete.Task;

    public string? GameNameOverride
    {
        get => gameNameOverride;
        set
        {
            if (gameNameOverride == value)
                return;
            gameNameOverride = value;
            OnPropertyChanged();
            CommitUserUpdate(e => e.GameNameOverride, value);
        }
    }

    public bool IsLoadingScrapbookImages
    {
        get => isLoadingScrapbookImages;
        private set => StaticDispatcher.Dispatch(() => SetBackedProperty(ref isLoadingScrapbookImages, in value));
    }

    public DateTimeOffset LatestLastWriteTime
    {
        get => latestLastWriteTime;
        private set
        {
            if (latestLastWriteTime == value)
                return;
            latestLastWriteTime = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => name;
        set
        {
            if (name == value)
                return;
            name = value;
            OnPropertyChanged();
            CommitUserUpdate(e => e.Name, value);
        }
    }

    public string? Notes
    {
        get => notes;
        set
        {
            if (notes == value)
                return;
            notes = value;
            OnPropertyChanged();
            CommitUserUpdate(e => e.Notes, value);
        }
    }

    public ulong NucleusId =>
        nucleusId;

    public IObservableCollectionQuery<ScrapbookImage> ScrapbookImages =>
        scrapbookImagesQuery;

    public ReadOnlyObservableCollection<Snapshot> Snapshots { get; }

    public ImmutableArray<byte> Thumbnail
    {
        get => thumbnail;
        set
        {
            if (thumbnail.SequenceEqual(value))
                return;
            thumbnail = value;
            OnPropertyChanged();
            CommitUserUpdate(e => e.Thumbnail, [..value]);
        }
    }

    public IReadOnlyCollection<SavePackageSnapshotDefectType>? WatchedSnapshotDefects
    {
        get => watchedSnapshotDefects;
        set
        {
            value ??= [];
            watchedSnapshotDefects = value;
            OnPropertyChanged();
            _ = Task.Run(RefreshWatchedSnapshotDefects);
        }
    }

    public async Task BrowseForCustomThumbnailAsync(IDialogService dialogService)
    {
        if (await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = AppText.Archivist_SelectCustomThumbnail_Caption,
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(false) is { } fileResult)
            try
            {
                using var fileStream = await fileResult.OpenReadAsync().ConfigureAwait(false);
                await SetThumbnailAsync(dialogService, await Image.LoadAsync<Rgba32>(fileStream).ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorDialogAsync(AppText.Archivist_SelectCustomThumbnail_Failed, $"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
            }
    }

    public async Task ClearThumbnailAsync()
    {
        try
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is { } propertySet)
            {
                propertySet.Thumbnail = [];
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                Thumbnail = [];
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while clearing thumbnail for nucleus ID {NucleusId}, created {Created}", nucleusId, created);
        }
    }

    void CommitUserUpdate<T>(Expression<Func<ChroniclePropertySet, T>> expression, T value) =>
        _ = Task.Run(async () =>
        {
            try
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                var propertySet = new ChroniclePropertySet { Id = 1 };
                dbContext.Attach(propertySet);
                dbContext.Entry(propertySet).Property(expression).CurrentValue = value;
                dbContext.Entry(propertySet).Property(expression).IsModified = true;
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unexpected unhandled exception while applying user update {Expression} for nucleus ID {NucleusId}, created {Created}", expression, nucleusId, created);
            }
        });

    async Task LoadAllAsync()
    {
        await ReloadScalarsAsync().ConfigureAwait(false);
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        watchedSnapshotDefects = Enum.GetValues<SavePackageSnapshotDefectType>().Except([SavePackageSnapshotDefectType.None]).Except(await dbContext.DisabledSavePackageSnapshotDefectTypes.Select(dspsdt => dspsdt.SavePackageSnapshotDefectType).ToListAsync().ConfigureAwait(false)).ToList().AsReadOnly();
        await foreach (var savePackageSnapshot in dbContext.SavePackageSnapshots.Include(sps => sps.OriginalSavePackageHash).Include(sps => sps.EnhancedSavePackageHash).AsAsyncEnumerable())
            await LoadSnapshotAsync(savePackageSnapshot).ConfigureAwait(false);
        firstLoadComplete.SetResult();
    }

    public async Task<Snapshot?> LoadSnapshotAsync(SavePackageSnapshot savePackageSnapshot)
    {
        ArgumentNullException.ThrowIfNull(savePackageSnapshot);
        try
        {
            var snapshot = new Snapshot(loggerFactory.CreateLogger<Snapshot>(), dbContextFactory, savePackageSnapshot, this, collectionObserver, mainThreadDetails);
            await snapshot.FirstLoadComplete.ConfigureAwait(false);
            snapshots.Add(snapshot);
            LatestLastWriteTime = snapshots.Select(s => s.LastWriteTime).Max();
            if (snapshots.Count is 1)
                EarliestLastWriteTime = snapshots.Select(s => s.LastWriteTime).Min();
            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while loading snapshot {SnapshotId} for nucleus ID {NucleusId}, created {Created}", savePackageSnapshot.Id, nucleusId, created);
        }
        return null;
    }

    public void LoadScrapbookImages()
    {
        using var loadScrapbookImagesCancellationTokenSourceLockHeld = loadScrapbookImagesCancellationTokenSourceLock.Lock();
        loadScrapbookImagesCancellationTokenSource?.Cancel();
        loadScrapbookImagesCancellationTokenSource?.Dispose();
        loadScrapbookImagesCancellationTokenSource = new();
        var cancellationToken = loadScrapbookImagesCancellationTokenSource.Token;
        _ = Task.Run(() => ReloadScrapbookImagesAsync(cancellationToken));
    }

    public async Task ReloadScalarsAsync()
    {
        try
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            DatabaseSize = await dbContext.Database.SqlQueryRaw<long>("PRAGMA page_count;").AsAsyncEnumerable().FirstOrDefaultAsync().ConfigureAwait(false)
                * await dbContext.Database.SqlQueryRaw<long>("PRAGMA page_size;").AsAsyncEnumerable().FirstOrDefaultAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                return;
            var basisNucleusId = MemoryMarshal.Read<ulong>(propertySet.BasisNucleusId ?? new byte[8]);
            var basisCreated = MemoryMarshal.Read<ulong>(propertySet.BasisCreated ?? new byte[8]);
            var basisOriginalPackageSha256 = (propertySet.BasisOriginalPackageSha256 ?? []).ToImmutableArray();
            if (this.basisNucleusId != basisNucleusId
                || this.basisCreated != basisCreated
                || !this.basisOriginalPackageSha256.SequenceEqual(basisOriginalPackageSha256))
            {
                this.basisNucleusId = basisNucleusId;
                this.basisCreated = basisCreated;
                this.basisOriginalPackageSha256 = basisOriginalPackageSha256;
                OnPropertyChanged(nameof(BasedOnSnapshot));
            }
            var created = MemoryMarshal.Read<ulong>(propertySet.Created ?? new byte[8]);
            if (this.created != created)
            {
                this.created = created;
                OnPropertyChanged(nameof(Created));
            }
            if (gameNameOverride != propertySet.GameNameOverride)
            {
                gameNameOverride = propertySet.GameNameOverride;
                OnPropertyChanged(nameof(GameNameOverride));
            }
            if (name != propertySet.Name)
            {
                name = propertySet.Name;
                OnPropertyChanged(nameof(Name));
            }
            if (notes != propertySet.Notes)
            {
                notes = propertySet.Notes;
                OnPropertyChanged(nameof(Notes));
            }
            var nucleusId = MemoryMarshal.Read<ulong>(propertySet.NucleusId ?? new byte[8]);
            if (this.nucleusId != nucleusId)
            {
                this.nucleusId = nucleusId;
                OnPropertyChanged(nameof(NucleusId));
            }
            Thumbnail = propertySet.Thumbnail.ToImmutableArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while reloading scalars for nucleus ID {NucleusId}, created {Created}", nucleusId, created);
        }
    }

    record ResourceKeyQueryRecord(byte[] Key);

    [SuppressMessage("Security", "EF1002: Risk of vulnerability to SQL injection.")]
    async Task ReloadScrapbookImagesAsync(CancellationToken cancellationToken)
    {
        using var loadScrapbookImagesLockHeld = await loadScrapbookImagesLock.LockAsync().ConfigureAwait(false);
        IsLoadingScrapbookImages = true;
        try
        {
            scrapbookImages.Clear();
            var saveGameDataBySnapshot = new NullableKeyDictionary<long?, (ReadOnlyMemory<byte> raw, SaveGameData deserialized)>();
            var saveGameDataBySnapshotLock = new AsyncLock();
            static MemoryStream fromReadOnlyMemory(ReadOnlyMemory<byte> data) =>
                  MemoryMarshal.TryGetArray(data, out var segment) && segment.Array is { } array
                ? new MemoryStream(array, segment.Offset, segment.Count, writable: false)
                : new MemoryStream(data.ToArray());
            async Task<ResourceKey?> getSaveGameDataResourceKeyForSnapshot(long? snapshotId)
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                var sgdTypeHex = BinaryPrimitives.ReverseEndianness((uint)ResourceType.SaveGameData).ToString("X8");
                string query;
                if (snapshotId is null)
                {
                    query =
                        $"""
                        SELECT
                        	spr.Key Key
                        FROM
                        	SavePackageResources spr
                        	LEFT JOIN ResourceSnapshotDeltas rsd ON rsd.SavePackageResourceId = spr.Id
                        WHERE
                        	substr(spr.Key, 1, 4) = X'{sgdTypeHex}'
                        GROUP BY
                        	spr.Key
                        HAVING
                        	count(rsd.Id) = 0
                        	OR max(rsd.Id) IN (
                        		SELECT
                        			max(rsd2.Id)
                        		FROM
                        			ResourceSnapshotDeltas rsd2
                        			JOIN SavePackageResources spr2 ON spr2.Id = rsd2.SavePackageResourceId
                        		WHERE
                        			substr(spr2.Key, 1, 4) = X'{sgdTypeHex}'
                        	)
                        """;
                }
                else
                {
                    query =
                        $"""
                        SELECT
                        	spr.Key Key
                        FROM
                        	SavePackageResources spr
                        	LEFT JOIN ResourceSnapshotDeltas rsd ON rsd.SavePackageResourceId = spr.Id
                        WHERE
                        	substr(spr.Key, 1, 4) = X'{sgdTypeHex}'
                            AND rsd.SavePackageSnapshotId <= {snapshotId}
                        GROUP BY
                        	spr.Key
                        HAVING
                        	count(rsd.Id) = 0
                        	OR max(rsd.Id) IN (
                        		SELECT
                        			max(rsd2.Id)
                        		FROM
                        			ResourceSnapshotDeltas rsd2
                        			JOIN SavePackageResources spr2 ON spr2.Id = rsd2.SavePackageResourceId
                        		WHERE
                        			substr(spr2.Key, 1, 4) = X'{sgdTypeHex}'
                                    AND rsd2.SavePackageSnapshotId <= {snapshotId}
                        	)
                        """;
                }
                if ((await dbContext.Database.SqlQueryRaw<ResourceKeyQueryRecord>(query).FirstOrDefaultAsync().ConfigureAwait(false))?.Key is not { } keyBytes
                    || keyBytes.Length is not 16)
                    return null;
                Span<byte> keySpan = keyBytes;
                return new ResourceKey
                (
                    MemoryMarshal.Read<ResourceType>(keySpan[0..4]),
                    MemoryMarshal.Read<uint>(keySpan[4..8]),
                    MemoryMarshal.Read<ulong>(keySpan[8..16])
                );
            }
            async Task<string> getScrapbookImageRevisionMetadataAsync(long? snapshotId, ResourceKey key)
            {
                using var saveGameDataBySnapshotLockHeld = await saveGameDataBySnapshotLock.LockAsync().ConfigureAwait(false);
                if (!saveGameDataBySnapshot.TryGetValue(snapshotId, out var sgdTuple))
                {
                    using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    if (await getSaveGameDataResourceKeyForSnapshot(snapshotId).ConfigureAwait(false) is not { } sgdKey)
                        return string.Empty;
                    var sgdKeyBytes = sgdKey.ToByteArray();
                    if (snapshotId is null)
                    {
                        if (await dbContext.SavePackageResources.FirstOrDefaultAsync(spr => spr.Key == sgdKeyBytes).ConfigureAwait(false) is not { } resource)
                            return string.Empty;
                        var raw = await DataBasePackedFile.ZLibDecompressAsync(resource.ContentZLib, resource.ContentSize).ConfigureAwait(false);
                        var deserialized = Serializer.Deserialize<SaveGameData>(raw);
                        sgdTuple = (raw, deserialized);
                        saveGameDataBySnapshot.Add(null, sgdTuple);
                    }
                }
                var (_, sgd) = sgdTuple;
                if (key.Type is ResourceType.MemorialThumbnail && sgd.Sims.FirstOrDefault(s => s.SimId == key.FullInstance) is { } deceasedSim)
                    return $"{deceasedSim.FirstName} {deceasedSim.LastName}";
                return string.Empty;
            }
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await foreach (var record in dbContext.Database.SqlQueryRaw<ResourceKeyQueryRecord>($"SELECT Key FROM SavePackageResources WHERE substr(Key, 1, 4) IN ({string.Join(", ", new ResourceType[] { ResourceType.SaveGameCustomTexture, ResourceType.SaveGameSimCustomTexture, ResourceType.MemorialThumbnail }.Select(rt => $"X'{BinaryPrimitives.ReverseEndianness((uint)rt):X8}'"))})").AsAsyncEnumerable().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (record.Key.Length is not 16)
                    continue;
                Span<byte> keySpan = record.Key;
                scrapbookImages.Add
                (
                    await ScrapbookImage.LoadAsync
                    (
                        loggerFactory.CreateLogger<ScrapbookImage>(),
                        dbContextFactory,
                        this,
                        collectionObserver,
                        mainThreadDetails,
                        new ResourceKey(MemoryMarshal.Read<ResourceType>(keySpan[0..4]), MemoryMarshal.Read<uint>(keySpan[4..8]), MemoryMarshal.Read<ulong>(keySpan[8..16])),
                        getScrapbookImageRevisionMetadataAsync
                    ).ConfigureAwait(false)
                );
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "reload scrapbook images");
        }
        finally
        {
            IsLoadingScrapbookImages = false;
        }
    }

    async Task RefreshWatchedSnapshotDefects()
    {
        var wsd = watchedSnapshotDefects ?? [];
        var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await dbContext.DisabledSavePackageSnapshotDefectTypes
            .Where(dspsdt => wsd.Contains(dspsdt.SavePackageSnapshotDefectType))
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
        await dbContext.DisabledSavePackageSnapshotDefectTypes.AddRangeAsync
        (
            Enum.GetValues<SavePackageSnapshotDefectType>()
                .Except([SavePackageSnapshotDefectType.None])
                .Except(wsd)
                .Except
                (
                    await dbContext.DisabledSavePackageSnapshotDefectTypes
                        .Select(dspsdt => dspsdt.SavePackageSnapshotDefectType)
                        .ToListAsync()
                        .ConfigureAwait(false)
                )
                .Select(spsdt => new DisabledSavePackageSnapshotDefectType
                {
                    SavePackageSnapshotDefectType = spsdt
                })
        ).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
        var disabled = (await dbContext.DisabledSavePackageSnapshotDefectTypes.Select(dspsdt => dspsdt.SavePackageSnapshotDefectType).ToListAsync().ConfigureAwait(false)).AsReadOnly();
        foreach (var snapshot in snapshots)
            await snapshot.ReloadDefectsAsync(disabled).ConfigureAwait(false);
    }

    public async Task SetThumbnailAsync(IDialogService dialogService, Image<Rgba32> thumbnail)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(thumbnail);
        try
        {
            if (thumbnail.Width > 2048 || thumbnail.Height > 2048)
                thumbnail.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new(2048, 2048),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
            using var thumbnailMemoryStream = new MemoryStream();
            await thumbnail.SaveAsPngAsync(thumbnailMemoryStream).ConfigureAwait(false);
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is { } propertySet)
            {
                propertySet.Thumbnail = thumbnailMemoryStream.ToArray();
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                Thumbnail = [..propertySet.Thumbnail];
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while setting thumbnail for nucleus ID {NucleusId}, created {Created}", nucleusId, created);
            await dialogService.ShowErrorDialogAsync("Set Thumbnail Failed", $"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
        }
    }

    public void UnloadScrapbookImages() =>
        _ = Task.Run(UnloadScrapbookImagesAsync);

    async Task UnloadScrapbookImagesAsync()
    {
        using (var loadScrapbookImagesCancellationTokenSourceLockHeld = await loadScrapbookImagesCancellationTokenSourceLock.LockAsync().ConfigureAwait(false))
        {
            loadScrapbookImagesCancellationTokenSource?.Cancel();
            loadScrapbookImagesCancellationTokenSource?.Dispose();
            loadScrapbookImagesCancellationTokenSource = null;
        }
        using var loadScrapbookImagesLockHeld = await loadScrapbookImagesLock.LockAsync().ConfigureAwait(false);
        scrapbookImages.Clear();
    }

    public void UnloadSnapshots(IEnumerable<Snapshot> snapshots)
    {
        foreach (var snapshot in snapshots.OrderBy(s => s.SavePackageSnapshotId))
            this.snapshots.Remove(snapshot);
        EarliestLastWriteTime = this.snapshots.Select(s => s.LastWriteTime).Min();
    }
}
