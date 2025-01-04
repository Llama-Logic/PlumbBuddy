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
    public Chronicle(IDbContextFactory<ChronicleDbContext> dbContextFactory, IArchivist archivist)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(archivist);
        this.dbContextFactory = dbContextFactory;
        this.archivist = archivist;
        snapshots = [];
        Snapshots = new(snapshots);
        firstLoadComplete = new();
        _ = Task.Run(LoadAllAsync);
    }

    readonly IArchivist archivist;
    ImmutableArray<byte> basisFullInstance = [];
    ImmutableArray<byte> basisOriginalPackageSha256 = [];
    readonly IDbContextFactory<ChronicleDbContext> dbContextFactory;
    readonly TaskCompletionSource firstLoadComplete;
    ImmutableArray<byte> fullInstance = [];
    DateTimeOffset? lastUpdated;
    string name = string.Empty;
    string? notes;
    int snapshotCount;
    readonly ObservableCollection<Snapshot> snapshots;
    string? thumbnailUri;

    public IArchivist Archivist =>
        archivist;

    public Snapshot? BasedOnSnapshot =>
        archivist.Chronicles.FirstOrDefault(c => c.FullInstance.SequenceEqual(basisFullInstance)) is { } basisChronicle
        && basisChronicle.Snapshots.FirstOrDefault(s => s.OriginalPackageSha256.SequenceEqual(basisOriginalPackageSha256)) is { } basisSnapshot
        ? basisSnapshot
        : null;

    public Task FirstLoadComplete =>
        firstLoadComplete.Task;

    public ImmutableArray<byte> FullInstance =>
        fullInstance;

    public DateTimeOffset? LastUpdated =>
        lastUpdated;

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

    public int SnapshotCount =>
        snapshotCount;

    public ReadOnlyObservableCollection<Snapshot> Snapshots { get; }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task BrowseForCustomThumbnailAsync(IDialogService dialogService)
    {
        if (await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a Custom Thumbnail",
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(false) is { } fileResult)
        {
            try
            {
                using var fileStream = await fileResult.OpenReadAsync().ConfigureAwait(false);
                var thumbnail = await Image.LoadAsync<Rgba32>(fileStream).ConfigureAwait(false);
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
                    ThumbnailUri = $"data:image/png;base64,{Convert.ToBase64String(propertySet.Thumbnail)}";
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowErrorDialogAsync("Set Thumbnail Failed", $"{ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
            }
        }
    }

    public async Task ClearThumbnailAsync()
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is { } propertySet)
        {
            propertySet.Thumbnail = [];
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            ThumbnailUri = null;
        }
    }

    void CommitUserUpdate<T>(Expression<Func<ChroniclePropertySet, T>> expression, T value) =>
        _ = Task.Run(async () =>
        {
            using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            var propertySet = new ChroniclePropertySet { Id = 1 };
            dbContext.Attach(propertySet);
            dbContext.Entry(propertySet).Property(expression).CurrentValue = value;
            dbContext.Entry(propertySet).Property(expression).IsModified = true;
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        });

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    async Task LoadAllAsync()
    {
        await ReloadScalarsAsync().ConfigureAwait(false);
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await foreach (var savePackageSnapshot in dbContext.SavePackageSnapshots.Include(sps => sps.OriginalSavePackageHash).AsAsyncEnumerable())
            await LoadSnapshotAsync(savePackageSnapshot).ConfigureAwait(false);
        firstLoadComplete.SetResult();
    }

    public async Task LoadSnapshotAsync(SavePackageSnapshot savePackageSnapshot)
    {
        var snapshot = new Snapshot(dbContextFactory, savePackageSnapshot, this);
        await snapshot.FirstLoadComplete.ConfigureAwait(false);
        snapshots.Add(snapshot);
    }

    public async Task ReloadScalarsAsync()
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        if (await dbContext.ChroniclePropertySets.FirstOrDefaultAsync().ConfigureAwait(false) is { } propertySet)
        {
            var basisFullInstance = (propertySet.BasisFullInstance ?? []).ToImmutableArray();
            var basisOriginalPackageSha256 = (propertySet.BasisOriginalPackageSha256 ?? []).ToImmutableArray();
            if (!this.basisFullInstance.SequenceEqual(basisFullInstance)
                || !this.basisOriginalPackageSha256.SequenceEqual(basisOriginalPackageSha256))
            {
                this.basisFullInstance = basisFullInstance;
                this.basisOriginalPackageSha256 = basisOriginalPackageSha256;
                OnPropertyChanged(nameof(BasedOnSnapshot));
            }
            var fullInstance = propertySet.FullInstance.ToImmutableArray();
            if (!this.fullInstance.SequenceEqual(fullInstance))
            {
                this.fullInstance = fullInstance;
                OnPropertyChanged(nameof(FullInstance));
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
            ThumbnailUri = propertySet.Thumbnail is { Length: > 0 } thumbnail
                ? $"data:image/png;base64,{Convert.ToBase64String(thumbnail)}"
                : null;
        }
        var currentLastUpdated = (await dbContext.SavePackageSnapshots
        .OrderByDescending(sps => sps.Id)
        .FirstOrDefaultAsync()
            .ConfigureAwait(false))
            ?.LastWriteTime;
        if (lastUpdated != currentLastUpdated)
        {
            lastUpdated = currentLastUpdated;
            OnPropertyChanged(nameof(LastUpdated));
        }
        var currentSnapshotCount = await dbContext.SavePackageSnapshots.CountAsync().ConfigureAwait(false);
        if (currentSnapshotCount != snapshotCount)
        {
            snapshotCount = currentSnapshotCount;
            OnPropertyChanged(nameof(SnapshotCount));
        }
    }
}
