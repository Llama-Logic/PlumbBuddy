using EA.Sims4.Persistence;
using Google.Protobuf;

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

    public Snapshot(IDbContextFactory<ChronicleDbContext> dbContextFactory, SavePackageSnapshot savePackageSnapshot, Chronicle chronicle)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(savePackageSnapshot);
        ArgumentNullException.ThrowIfNull(chronicle);
        this.dbContextFactory = dbContextFactory;
        savePackageSnapshotId = savePackageSnapshot.Id;
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
    string? notes;
    ImmutableArray<byte> originalPackageSha256 = [];
    readonly long savePackageSnapshotId;
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
                    var savePackageSnapshot = await dbContext.SavePackageSnapshots.FindAsync(savePackageSnapshotId).ConfigureAwait(false);
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
            var savePackageSnapshot = new SavePackageSnapshot { Id = savePackageSnapshotId };
            dbContext.Attach(savePackageSnapshot);
            dbContext.Entry(savePackageSnapshot).Property(expression).CurrentValue = value;
            dbContext.Entry(savePackageSnapshot).Property(expression).IsModified = true;
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        });

    public Task<FileInfo?> CreateBranchAsync(ISettings settings, Chronicle chronicle, string newChronicleName, uint newSaveGameInstance, Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(newChronicleName);
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                return null;
            using var package = await RegeneratePackageAsync(dbContext, savePackageSnapshotId).ConfigureAwait(false);
            var keys = await package.GetKeysAsync().ConfigureAwait(false);
            var slotId = GetOpenSlot(settings);
            foreach (var key in keys.Where(k => k.Type is ResourceType.SaveGameData))
            {
                var compressionMode = package.GetExplicitCompressionMode(key);
                var saveGameData = SaveGameData.Parser.ParseFrom((await package.GetAsync(key).ConfigureAwait(false)).Span);
                saveGameData.SaveSlot.SlotId = slotId;
                saveGameData.SaveSlot.SlotName = newChronicleName;
                package.Delete(key);
                await package.SetAsync(new ResourceKey(ResourceType.SaveGameData, key.Group, newSaveGameInstance), saveGameData.ToByteArray(), compressionMode).ConfigureAwait(false);
            }
            IDbContextFactory<ChronicleDbContext> newChronicleDbContextFactory = new ChronicleDbContextFactory(new FileInfo(Path.Combine(settings.ArchiveFolderPath, $"{new ResourceKey(default, default, newSaveGameInstance).FullInstanceHex}.chronicle.sqlite")));
            using var newChronicleDbContext = await newChronicleDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if ((await newChronicleDbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                await newChronicleDbContext.Database.MigrateAsync().ConfigureAwait(false);
            var longNewSaveGameInstance = (long)newSaveGameInstance;
            var newSaveGameInstanceBytes = new byte[8];
            Span<byte> newSaveGameInstanceBytesSpan = newSaveGameInstanceBytes;
            MemoryMarshal.Write(newSaveGameInstanceBytesSpan, in longNewSaveGameInstance);
            await newChronicleDbContext.ChroniclePropertySets.AddAsync(new()
            {
                BasisFullInstance = propertySet.FullInstance,
                BasisOriginalPackageSha256 = [.. originalPackageSha256],
                FullInstance = newSaveGameInstanceBytes,
                Name = newChronicleName,
                Thumbnail = propertySet.Thumbnail
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
        });
    }

    public async Task<FileInfo?> ExportModListAsync()
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync();
        if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync() is not { } propertySet)
            return null;
        using var csvStream = new MemoryStream();
        using var csvStreamWriter = new StreamWriter(csvStream);
        using var csvWriter = new CsvWriter(csvStreamWriter, new CsvConfiguration(CultureInfo.InvariantCulture));
        await csvWriter.WriteRecordsAsync((await dbContext.SavePackageSnapshots
            .Where(sps => sps.Id == savePackageSnapshotId)
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

    public Task<FileInfo?> ExportSavePackageAsync(Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                return null;
            using var package = await RegeneratePackageAsync(dbContext, savePackageSnapshotId).ConfigureAwait(false);
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
        if (savePackageSnapshot is null)
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            savePackageSnapshot = await dbContext.SavePackageSnapshots
                .Include(sps => sps.EnhancedSavePackageHash)
                .Include(sps => sps.OriginalSavePackageHash)
                .FirstOrDefaultAsync(sps => sps.Id == savePackageSnapshotId)
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

    public Task<FileInfo?> RestoreSavePackageAsync(ISettings settings, Chronicle chronicle, Action onSerializationCompleted)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(onSerializationCompleted);
        return Task.Run(async () =>
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is not { } propertySet)
                return null;
            using var package = await RegeneratePackageAsync(dbContext, savePackageSnapshotId).ConfigureAwait(false);
            var keys = await package.GetKeysAsync().ConfigureAwait(false);
            foreach (var key in keys.Where(k => k.Type is ResourceType.SaveGameData))
            {
                var saveGameData = SaveGameData.Parser.ParseFrom((await package.GetAsync(key).ConfigureAwait(false)).Span);
                saveGameData.SaveSlot.SlotName = $"{chronicle.Name}: {Label}";
                await package.SetAsync(key, saveGameData.ToByteArray(), package.GetExplicitCompressionMode(key)).ConfigureAwait(false);
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
        });
    }
}
