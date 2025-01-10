using EA.Sims4.Persistence;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public class Snapshot :
    INotifyPropertyChanged
{
    static async Task<MemoryStream> ConvertPackageToExcludedStreamAsync(ChronicleDbContext dbContext, DataBasePackedFile package)
    {
        var packageStream = new MemoryStream();
        await package.CopyToAsync(packageStream).ConfigureAwait(false);
        using (var sha256 = SHA256.Create())
        {
            packageStream.Seek(0, SeekOrigin.Begin);
            var packageSha256 = await sha256.ComputeHashAsync(packageStream).ConfigureAwait(false);
            if (!await dbContext.KnownSavePackageHashes.AnyAsync(esp => esp.Sha256 == packageSha256).ConfigureAwait(false))
            {
                await dbContext.KnownSavePackageHashes.AddAsync(new() { Sha256 = packageSha256 }).ConfigureAwait(false);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        return packageStream;
    }

    static uint GetOpenSlot(ISettings settings)
    {
        var savesDirectoryFiles = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"))
            .GetFiles("*.*", SearchOption.TopDirectoryOnly);
        uint slotId = 0x0002;
        var slotIdHex = slotId.ToString("x8");
        while (savesDirectoryFiles.Any(f => f.Name.StartsWith($"Slot_{slotIdHex}")))
            slotIdHex = (++slotId).ToString("x8");
        return slotId;
    }

    static async Task<DataBasePackedFile> RegeneratePackageAsync(ChronicleDbContext dbContext, long savePackageSnapshotId)
    {
        static MemoryStream fromReadOnlyMemory(ReadOnlyMemory<byte> data) =>
            MemoryMarshal.TryGetArray(data, out var segment) && segment.Array is { } array
            ? new MemoryStream(array, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(data.ToArray());
        var package = new DataBasePackedFile();
        using (var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 4)))
            await Task.WhenAll((await dbContext.SavePackageResources
                .Where(spr => spr.Snapshots!.Any(s => s.Id == savePackageSnapshotId))
                .Select(spr => new
                {
                    spr.Key,
                    spr.CompressionType,
                    spr.ContentZLib,
                    spr.ContentSize,
                    Deltas = spr.Deltas!
                        .Where(d => d.SavePackageSnapshotId > savePackageSnapshotId)
                        .OrderByDescending(d => d.SavePackageSnapshotId)
                        .Select(d => new
                        {
                            d.PatchZLib,
                            d.PatchSize
                        })
                        .ToList()
                })
                .ToListAsync()
                .ConfigureAwait(false)).Select(async resource =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var contentStream = fromReadOnlyMemory(await DataBasePackedFile.ZLibDecompressAsync(resource.ContentZLib, resource.ContentSize).ConfigureAwait(false));
                        try
                        {
                            foreach (var delta in resource.Deltas)
                            {
                                using var oldContentStream = contentStream;
                                contentStream = new MemoryStream();
                                var patch = await DataBasePackedFile.ZLibDecompressAsync(delta.PatchZLib, delta.PatchSize).ConfigureAwait(false);
                                BinaryPatch.Apply(oldContentStream, () => fromReadOnlyMemory(patch), contentStream);
                            }
                            Span<byte> keyBytes = resource.Key;
                            await package.SetAsync
                            (
                                new ResourceKey
                                (
                                    MemoryMarshal.Read<ResourceType>(keyBytes[0..4]),
                                    MemoryMarshal.Read<uint>(keyBytes[4..8]),
                                    MemoryMarshal.Read<ulong>(keyBytes[8..16])
                                ),
                                contentStream.ToArray().AsMemory(),
                                resource.CompressionType switch
                                {
                                    SavePackageResourceCompressionType.None => LlamaLogic.Packages.CompressionMode.ForceOff,
                                    SavePackageResourceCompressionType.Deleted => LlamaLogic.Packages.CompressionMode.SetDeletedFlag,
                                    SavePackageResourceCompressionType.Internal => LlamaLogic.Packages.CompressionMode.ForceInternal,
                                    SavePackageResourceCompressionType.Streamable => LlamaLogic.Packages.CompressionMode.CallerSuppliedStreamable,
                                    SavePackageResourceCompressionType.ZLIB => LlamaLogic.Packages.CompressionMode.ForceZLib,
                                    _ => throw new NotSupportedException("unsupported DBPF compression")
                                }
                            ).ConfigureAwait(false);
                        }
                        finally
                        {
                            await contentStream.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })).ConfigureAwait(false);
        return package;
    }

    public Snapshot(ILogger<Snapshot> logger, IDbContextFactory<ChronicleDbContext> dbContextFactory, SavePackageSnapshot savePackageSnapshot, Chronicle chronicle)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(savePackageSnapshot);
        ArgumentNullException.ThrowIfNull(chronicle);
        this.logger = logger;
        this.dbContextFactory = dbContextFactory;
        SavePackageSnapshotId = savePackageSnapshot.Id;
        this.chronicle = chronicle;
        firstLoadComplete = new();
        _ = Task.Run(() => LoadAllAsync(savePackageSnapshot));
    }

    readonly Chronicle chronicle;
    readonly IDbContextFactory<ChronicleDbContext> dbContextFactory;
    ImmutableArray<byte> enhancedPackageSha256 = [];
    readonly TaskCompletionSource firstLoadComplete;
    bool isEditing;
    string label = string.Empty;
    readonly ILogger<Snapshot> logger;
    string? notes;
    ImmutableArray<byte> originalPackageSha256 = [];
    bool showDetails;
    string? thumbnailUri;

    public string? ActiveHouseholdName { get; private set; }

    public Chronicle Chronicle =>
        chronicle;

    public ImmutableArray<byte> EnhancedPackageSha256 =>
        enhancedPackageSha256;

    public Task FirstLoadComplete =>
        firstLoadComplete.Task;

    public bool IsEditing
    {
        get => isEditing;
        set
        {
            if (isEditing == value)
                return;
            isEditing = value;
            OnPropertyChanged();
        }
    }

    public string Label
    {
        get => label;
        set
        {
            if (label == value)
                return;
            label = value;
            OnPropertyChanged();
            CommitUserUpdate(e => e.Label, value);
        }
    }

    public string? LastPlayedLotName { get; private set; }

    public string? LastPlayedWorldName { get; private set; }

    public DateTimeOffset LastWriteTime { get; private set; }

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

    public ImmutableArray<byte> OriginalPackageSha256 =>
        originalPackageSha256;

    public long SavePackageSnapshotId { get; }

    public bool ShowDetails
    {
        get => showDetails;
        set
        {
            if (showDetails == value)
                return;
            showDetails = value;
            OnPropertyChanged();
            if (value)
                _ = Task.Run(async () =>
                {
                    using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    var savePackageSnapshot = await dbContext.SavePackageSnapshots.FindAsync(SavePackageSnapshotId).ConfigureAwait(false);
                    if (savePackageSnapshot?.Thumbnail is { Length: > 0 } thumbnail)
                        ThumbnailUri = $"data:image/png;base64,{Convert.ToBase64String(thumbnail)}";
                    else
                        ThumbnailUri = null;
                });
            else
                ThumbnailUri = null;
        }
    }

    [SuppressMessage("Design", "CA1056: URI-like properties should not be strings")]
    public string? ThumbnailUri
    {
        get => thumbnailUri;
        private set
        {
            if (thumbnailUri == value)
                return;
            thumbnailUri = value;
            OnPropertyChanged();
        }
    }

    public bool WasLive { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    void CommitUserUpdate<T>(Expression<Func<SavePackageSnapshot, T>> expression, T value) =>
        _ = Task.Run(async () =>
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var savePackageSnapshot = new SavePackageSnapshot { Id = SavePackageSnapshotId };
            dbContext.Attach(savePackageSnapshot);
            dbContext.Entry(savePackageSnapshot).Property(expression).CurrentValue = value;
            dbContext.Entry(savePackageSnapshot).Property(expression).IsModified = true;
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        });

    public Task<FileInfo?> CreateBranchAsync(ISettings settings, Chronicle chronicle, string newChronicleName, string? notes, string? gameNameOverride, ImmutableArray<byte> thumbnail, Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(newChronicleName);
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            try
            {
                var created = (ulong)DateTimeOffset.Now.ToUnixTimeSeconds();
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                    return null;
                using var package = await RegeneratePackageAsync(dbContext, SavePackageSnapshotId).ConfigureAwait(false);
                var packageKeys = await package.GetKeysAsync().ConfigureAwait(false);
                var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                if (saveGameDataKeys.Length is <= 0 or >= 2)
                    throw new FormatException("save package contains invalid number of save game data resources");
                var saveGameDataKey = saveGameDataKeys[0];
                var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await package.GetAsync(saveGameDataKey).ConfigureAwait(false));
                var compressionMode = package.GetExplicitCompressionMode(saveGameDataKey);
                var slotId = GetOpenSlot(settings);
                if (saveGameData.Account is not  { } account)
                    throw new System.MissingFieldException("save game data does not contain account");
                account.Created = created;
                if (saveGameData.SaveSlot is not { } saveSlot)
                    throw new System.MissingFieldException("save game data does not contain save slot");
                saveSlot.SlotId = slotId;
                saveSlot.SlotName = newChronicleName;
                await package.SetAsync(saveGameDataKey, saveGameData.ToProtobufMessage(), compressionMode).ConfigureAwait(false);
                var nucleusId = account.NucleusId;
                var nucleusIdBytes = new byte[8];
                Span<byte> nucleusIdBytesSpan = nucleusIdBytes;
                MemoryMarshal.Write(nucleusIdBytesSpan, in nucleusId);
                var createdBytes = new byte[8];
                Span<byte> createdBytesSpan = createdBytes;
                MemoryMarshal.Write(createdBytesSpan, in created);
                IDbContextFactory<ChronicleDbContext> newChronicleDbContextFactory = new ChronicleDbContextFactory(new FileInfo(Path.Combine(settings.ArchiveFolderPath, $"N-{nucleusId:x16}-C-{created:x16}.chronicle.sqlite")));
                using var newChronicleDbContext = await newChronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if ((await newChronicleDbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                    await newChronicleDbContext.Database.MigrateAsync().ConfigureAwait(false);
                await newChronicleDbContext.ChroniclePropertySets.AddAsync(new()
                {
                    BasisCreated = propertySet.Created,
                    BasisNucleusId = propertySet.NucleusId,
                    BasisOriginalPackageSha256 = [.. originalPackageSha256],
                    Created = createdBytes,
                    GameNameOverride = gameNameOverride,
                    Name = newChronicleName,
                    Notes = notes,
                    NucleusId = nucleusIdBytes,
                    Thumbnail = [..thumbnail]
                }).ConfigureAwait(false);
                await newChronicleDbContext.SaveChangesAsync().ConfigureAwait(false);
                var slotIdHex = slotId.ToString("x8");
                foreach (var existingFile in new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"))
                    .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Name.StartsWith(slotIdHex)))
                    existingFile.Delete();
                var filePath = Path.Combine(settings.UserDataFolderPath, "saves", $"Slot_{slotIdHex}.save");
                var tempPath = $"{filePath}.tmp";
                await package.SaveAsAsync(tempPath).ConfigureAwait(false);
                File.Move(tempPath, filePath);
                onSerializationCompleted();
                return new FileInfo(filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unexpected unhandled exception while branching from nucleus ID {NucleusId}, created {Created}, snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
                return null;
            }
        });
    }

    public Task<bool> DeletePreviousSnapshotsAsync() =>
        Task.Run(async () =>
        {
            try
            {
                using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                {
                    await dbContext.ResourceSnapshotDeltas
                        .Where(rsd => rsd.SavePackageSnapshotId <= SavePackageSnapshotId)
                        .ExecuteDeleteAsync()
                        .ConfigureAwait(false);
                    await dbContext.SavePackageSnapshots
                        .Where(d => d.Id < SavePackageSnapshotId)
                        .ExecuteDeleteAsync()
                        .ConfigureAwait(false);
                    await dbContext.SavePackageResources
                        .Where(spr => spr.Snapshots!.Count() == 0)
                        .ExecuteDeleteAsync()
                        .ConfigureAwait(false);
                    Chronicle.UnloadSnapshots(Chronicle.Snapshots.Where(s => s.SavePackageSnapshotId < SavePackageSnapshotId).ToImmutableArray());
                    await dbContext.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);").ConfigureAwait(false);
                    await dbContext.Database.ExecuteSqlRawAsync("VACUUM;").ConfigureAwait(false);
                }
                await Chronicle.ReloadScalarsAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unexpected unhandled exception while deleting snapshots in nucleus ID {NucleusId}, created {Created} prior to snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
                return false;
            }
        });

    public async Task<FileInfo?> ExportModListAsync()
    {
        try
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync();
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync() is not { } propertySet)
                return null;
            using var csvStream = new MemoryStream();
            using var csvStreamWriter = new StreamWriter(csvStream);
            using var csvWriter = new CsvWriter(csvStreamWriter, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csvWriter.WriteRecordsAsync((await dbContext.SavePackageSnapshots
                .Where(sps => sps.Id == SavePackageSnapshotId)
                .SelectMany(sps => sps.ModFiles!)
                .OrderBy(mf => mf.Path)
                .ToListAsync())
                .Select(mf => new
                {
                    mf.Path,
                    mf.Size,
                    mf.LastWriteTime,
                    mf.Sha256
                }));
            await csvWriter.FlushAsync();
            var result = await FileSaver.Default.SaveAsync(string.Concat($"{propertySet.Name} - {Label} Mod Files.csv".Split(Path.GetInvalidFileNameChars())), csvStream);
            if (!result.IsSuccessful)
                return null;
            if (result.FilePath is not { } filePath)
                return null;
            var file = new FileInfo(filePath);
            if (!file.Exists)
                return null;
            return file;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while exporting mod list for nucleus ID {NucleusId}, created {Created}, snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
            return null;
        }
    }

    public Task<FileInfo?> ExportSavePackageAsync(Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            try
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                    return null;
                using var package = await RegeneratePackageAsync(dbContext, SavePackageSnapshotId).ConfigureAwait(false);
                using var packageStream = await ConvertPackageToExcludedStreamAsync(dbContext, package).ConfigureAwait(false);
                onSerializationCompleted();
                var result = await FileSaver.Default.SaveAsync(string.Concat($"{propertySet.Name} - {Label}.save".Split(Path.GetInvalidFileNameChars())), packageStream);
                if (!result.IsSuccessful)
                    return null;
                if (result.FilePath is not { } filePath)
                    return null;
                var file = new FileInfo(filePath);
                if (!file.Exists)
                    return null;
                return file;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unexpected unhandled exception while exporting nucleus ID {NucleusId}, created {Created}, snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
                return null;
            }
        });
    }

    async Task LoadAllAsync(SavePackageSnapshot savePackageSnapshot)
    {
        await ReloadScalarsAsync(savePackageSnapshot).ConfigureAwait(false);
        firstLoadComplete.SetResult();
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    public async Task ReloadScalarsAsync(SavePackageSnapshot? savePackageSnapshot = null)
    {
        try
        {
            if (savePackageSnapshot is null)
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                savePackageSnapshot = await dbContext.SavePackageSnapshots
                    .Include(sps => sps.EnhancedSavePackageHash)
                    .Include(sps => sps.OriginalSavePackageHash)
                    .FirstOrDefaultAsync(sps => sps.Id == SavePackageSnapshotId)
                    .ConfigureAwait(false);
            }
            if (savePackageSnapshot is null)
                return;
            if (ActiveHouseholdName != savePackageSnapshot.ActiveHouseholdName)
            {
                ActiveHouseholdName = savePackageSnapshot.ActiveHouseholdName;
                OnPropertyChanged(nameof(ActiveHouseholdName));
            }
            var spsEnhancedSavePackageHash = (savePackageSnapshot.EnhancedSavePackageHash?.Sha256 ?? []).ToImmutableArray();
            if (!enhancedPackageSha256.SequenceEqual(spsEnhancedSavePackageHash))
            {
                enhancedPackageSha256 = spsEnhancedSavePackageHash;
                OnPropertyChanged(nameof(EnhancedPackageSha256));
            }
            if (label != savePackageSnapshot.Label)
            {
                label = savePackageSnapshot.Label;
                OnPropertyChanged(nameof(Label));
            }
            if (LastPlayedLotName != savePackageSnapshot.LastPlayedLotName)
            {
                LastPlayedLotName = savePackageSnapshot.LastPlayedLotName;
                OnPropertyChanged(nameof(LastPlayedLotName));
            }
            if (LastPlayedWorldName != savePackageSnapshot.LastPlayedWorldName)
            {
                LastPlayedWorldName = savePackageSnapshot.LastPlayedWorldName;
                OnPropertyChanged(nameof(LastPlayedWorldName));
            }
            if (LastWriteTime != savePackageSnapshot.LastWriteTime)
            {
                LastWriteTime = savePackageSnapshot.LastWriteTime;
                OnPropertyChanged(nameof(LastWriteTime));
            }
            if (notes != savePackageSnapshot.Notes)
            {
                notes = savePackageSnapshot.Notes;
                OnPropertyChanged(nameof(Notes));
            }
            var spsOriginalSavePackageHash = (savePackageSnapshot.OriginalSavePackageHash?.Sha256 ?? []).ToImmutableArray();
            if (!originalPackageSha256.SequenceEqual(spsOriginalSavePackageHash))
            {
                originalPackageSha256 = spsOriginalSavePackageHash;
                OnPropertyChanged(nameof(OriginalPackageSha256));
            }
            if (WasLive != savePackageSnapshot.WasLive)
            {
                WasLive = savePackageSnapshot.WasLive;
                OnPropertyChanged(nameof(WasLive));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while reloading scalars for nucleus ID {NucleusId}, created {Created}, snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
        }
    }

    public Task<FileInfo?> RestoreSavePackageAsync(ISettings settings, Chronicle chronicle, Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            try
            {
                using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                    return null;
                using var package = await RegeneratePackageAsync(dbContext, SavePackageSnapshotId).ConfigureAwait(false);
                var keys = await package.GetKeysAsync().ConfigureAwait(false);
                foreach (var key in keys.Where(k => k.Type is ResourceType.SaveGameData))
                {
                    var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await package.GetAsync(key).ConfigureAwait(false));
                    (saveGameData.SaveSlot ??= new()).SlotName = string.IsNullOrWhiteSpace(chronicle.GameNameOverride)
                        ? $"{chronicle.Name}: {Label}"
                        : chronicle.GameNameOverride.Trim();
                    await package.SetAsync(key, saveGameData.ToProtobufMessage(), package.GetExplicitCompressionMode(key)).ConfigureAwait(false);
                }
                ReadOnlyMemory<byte> thumbnail = propertySet.Thumbnail;
                if (!thumbnail.IsEmpty)
                    foreach (var saveThumbnail4Key in keys.Where(key => key.Type is ResourceType.SaveThumbnail4))
                        await package.SetPngAsTranslucentJpegAsync(saveThumbnail4Key, thumbnail).ConfigureAwait(false);
                using var packageStream = await ConvertPackageToExcludedStreamAsync(dbContext, package).ConfigureAwait(false);
                var slotId = GetOpenSlot(settings);
                var slotIdHex = slotId.ToString("x8");
                foreach (var existingFile in new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"))
                    .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Name.StartsWith(slotIdHex)))
                    existingFile.Delete();
                var filePath = Path.Combine(settings.UserDataFolderPath, "saves", $"Slot_{slotIdHex}.save");
                var tempPath = $"{filePath}.tmp";
                using (var packageFileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    packageStream.Seek(0, SeekOrigin.Begin);
                    await packageStream.CopyToAsync(packageFileStream).ConfigureAwait(false);
                    await packageFileStream.FlushAsync().ConfigureAwait(false);
                    packageFileStream.Close();
                }
                File.Move(tempPath, filePath);
                onSerializationCompleted();
                return new FileInfo(filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unexpected unhandled exception while restoring nucleus ID {NucleusId}, created {Created}, snapshot {SnapshotId}", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
                return null;
            }
        });
    }

    public async Task UseThumbnailForChronicleAsync(IDialogService dialogService)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        if (!Chronicle.Thumbnail.IsDefaultOrEmpty
            && !await dialogService.ShowCautionDialogAsync(AppText.Archivist_Snapshot_UseThumbnailForChronicle_Caution_Caption, AppText.Archivist_Snapshot_UseThumbnailForChronicle_Caution_Text))
            return;
        try
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var savePackageSnapshot = await dbContext.SavePackageSnapshots.FindAsync(SavePackageSnapshotId).ConfigureAwait(false);
            if (savePackageSnapshot?.Thumbnail is { Length: > 0 } thumbnail)
            {
                using var memoryStream = new MemoryStream(thumbnail, false);
                await chronicle.SetThumbnailAsync(dialogService, await Image.LoadAsync<Rgba32>(memoryStream).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while setting nucleus ID {NucleusId}, created {Created} thumbnail from snapshot {SnapshotId} original", chronicle.NucleusId, chronicle.Created, SavePackageSnapshotId);
            await dialogService.ShowErrorDialogAsync(AppText.Archivist_SelectCustomThumbnail_Failed, $"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
        }
    }
}
