using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using ResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace PlumbBuddy.Services;

[SuppressMessage("Naming", "CA1724: Type names should not match namespaces")]
public class Chronicle :
    INotifyPropertyChanged
{
    public Chronicle(ILoggerFactory loggerFactory, ILogger<Chronicle> logger, IDbContextFactory<ChronicleDbContext> dbContextFactory, IArchivist archivist)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(archivist);
        this.loggerFactory = loggerFactory;
        this.logger = logger;
        this.dbContextFactory = dbContextFactory;
        this.archivist = archivist;
        snapshots = [];
        Snapshots = new(snapshots);
        firstLoadComplete = new();
        _ = Task.Run(LoadAllAsync);
    }

    readonly IArchivist archivist;
    ulong basisCreated;
    ulong basisNucleusId;
    ImmutableArray<byte> basisOriginalPackageSha256 = [];
    ulong created;
    long databaseSize;
    readonly IDbContextFactory<ChronicleDbContext> dbContextFactory;
    DateTimeOffset earliestLastWriteTime;
    readonly TaskCompletionSource firstLoadComplete;
    string? gameNameOverride;
    DateTimeOffset latestLastWriteTime;
    readonly ILogger<Chronicle> logger;
    readonly ILoggerFactory loggerFactory;
    string name = string.Empty;
    string? notes;
    ulong nucleusId;
    readonly ObservableCollection<Snapshot> snapshots;
    ImmutableArray<byte> thumbnail = [];

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task BrowseForCustomThumbnailAsync(IDialogService dialogService)
    {
        if (await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = AppText.Archivist_SelectCustomThumbnail_Caption,
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(false) is { } fileResult)
        {
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

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    async Task LoadAllAsync()
    {
        await ReloadScalarsAsync().ConfigureAwait(false);
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var savePackageSnapshot in dbContext.SavePackageSnapshots.Include(sps => sps.OriginalSavePackageHash).Include(sps => sps.EnhancedSavePackageHash).AsAsyncEnumerable())
            await LoadSnapshotAsync(savePackageSnapshot).ConfigureAwait(false);
        firstLoadComplete.SetResult();
    }

    public async Task LoadSnapshotAsync(SavePackageSnapshot savePackageSnapshot)
    {
        ArgumentNullException.ThrowIfNull(savePackageSnapshot);
        try
        {
            var snapshot = new Snapshot(loggerFactory.CreateLogger<Snapshot>(), dbContextFactory, savePackageSnapshot, this);
            await snapshot.FirstLoadComplete.ConfigureAwait(false);
            snapshots.Add(snapshot);
            LatestLastWriteTime = snapshots.Select(s => s.LastWriteTime).Max();
            if (snapshots.Count is 1)
                EarliestLastWriteTime = snapshots.Select(s => s.LastWriteTime).Min();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "encountered unexpected unhandled exception while loading snapshot {SnapshotId} for nucleus ID {NucleusId}, created {Created}", savePackageSnapshot.Id, nucleusId, created);
        }
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

    public void UnloadSnapshots(IEnumerable<Snapshot> snapshots)
    {
        foreach (var snapshot in snapshots.OrderBy(s => s.SavePackageSnapshotId))
            this.snapshots.Remove(snapshot);
        EarliestLastWriteTime = this.snapshots.Select(s => s.LastWriteTime).Min();
    }
}
