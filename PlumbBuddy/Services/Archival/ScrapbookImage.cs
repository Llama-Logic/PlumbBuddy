using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using Image = SixLabors.ImageSharp.Image;

namespace PlumbBuddy.Services.Archival;

public sealed partial class ScrapbookImage :
    PropertyChangeNotifier
{
    public static async Task<ScrapbookImage> LoadAsync(ILogger<ScrapbookImage> logger, IDbContextFactory<ChronicleDbContext> dbContextFactory, Chronicle chronicle, ICollectionObserver collectionObserver, IMainThreadDetails mainThreadDetails, ResourceKey key, AsyncScrapbookImageRevisionMetadataRetrievalDelegate getScrapbookImageRevisionMetadataAsync)
    {
        var image = new ScrapbookImage(logger, dbContextFactory, chronicle, collectionObserver, mainThreadDetails, key, getScrapbookImageRevisionMetadataAsync);
        await image.InitializeAsync().ConfigureAwait(false);
        return image;
    }

    ScrapbookImage(ILogger<ScrapbookImage> logger, IDbContextFactory<ChronicleDbContext> dbContextFactory, Chronicle chronicle, ICollectionObserver collectionObserver, IMainThreadDetails mainThreadDetails, ResourceKey key, AsyncScrapbookImageRevisionMetadataRetrievalDelegate getScrapbookImageRevisionMetadataAsync)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(chronicle);
        ArgumentNullException.ThrowIfNull(collectionObserver);
        ArgumentNullException.ThrowIfNull(mainThreadDetails);
        ArgumentNullException.ThrowIfNull(getScrapbookImageRevisionMetadataAsync);
        this.logger = logger;
        this.dbContextFactory = dbContextFactory;
        this.chronicle = chronicle;
        this.collectionObserver = collectionObserver;
        this.mainThreadDetails = mainThreadDetails;
        Key = key;
        this.getScrapbookImageRevisionMetadataAsync = getScrapbookImageRevisionMetadataAsync;
        revisions = [];
        Revisions = collectionObserver.ObserveReadOnlyList(revisions).ObserveUsingSynchronizationContextEventually(mainThreadDetails.SynchronizationContext);
        _ = Task.Run(InitializeAsync);
    }

    readonly Chronicle chronicle;
    readonly ICollectionObserver collectionObserver;
    SavePackageResourceCompressionType? compressionType;
    readonly IDbContextFactory<ChronicleDbContext> dbContextFactory;
    string? description;
    readonly AsyncScrapbookImageRevisionMetadataRetrievalDelegate getScrapbookImageRevisionMetadataAsync;
    ResourceFileType? imageType;
    readonly ILogger<ScrapbookImage> logger;
    readonly IMainThreadDetails mainThreadDetails;
    ImmutableArray<byte> imageData;
    readonly ObservableCollection<ScrapbookImageRevision> revisions;

    public string? Description
    {
        get => description;
        private set => SetBackedProperty(ref description, in value);
    }

    public ImmutableArray<byte> ImageData
    {
        get => imageData;
        private set => SetBackedProperty(ref imageData, in value);
    }

    public ResourceFileType? ImageType
    {
        get => imageType;
        private set => SetBackedProperty(ref imageType, in value);
    }

    public ResourceKey Key { get; }

    public IObservableCollectionQuery<ScrapbookImageRevision> Revisions { get; }

    async Task InitializeAsync()
    {
        using var dbContext = await dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var keyBytes = Key.ToByteArray();
        if (await dbContext.SavePackageResources.Include(spr => spr.Deltas).ThenInclude(sprd => sprd.SavePackageSnapshot).FirstOrDefaultAsync(spr => spr.Key == keyBytes).ConfigureAwait(false) is not { } resource)
            return;
        var content = await DataBasePackedFile.ZLibDecompressAsync(resource.ContentZLib, resource.ContentSize).ConfigureAwait(false);
        compressionType = resource.CompressionType;
        await InitializeCurrentRevision(content).ConfigureAwait(false);
        if (ImageType is not { } imageType)
            return;
        Description = await getScrapbookImageRevisionMetadataAsync(null, Key).ConfigureAwait(false);
        static MemoryStream fromReadOnlyMemory(ReadOnlyMemory<byte> data) =>
              MemoryMarshal.TryGetArray(data, out var segment) && segment.Array is { } array
            ? new MemoryStream(array, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(data.ToArray());
        if (resource.Deltas.Count is > 0)
        {
            var contentStream = fromReadOnlyMemory(content);
            foreach (var delta in resource.Deltas.OrderByDescending(d => d.SavePackageSnapshot.Id))
            {
                using var oldContentStream = contentStream;
                contentStream = new MemoryStream();
                var patch = await DataBasePackedFile.ZLibDecompressAsync(delta.PatchZLib, delta.PatchSize).ConfigureAwait(false);
                BinaryPatch.Apply(oldContentStream, () => fromReadOnlyMemory(patch), contentStream);
                var snapshotId = delta.SavePackageSnapshot.Id;
                revisions.Add(new(snapshotId, delta.SavePackageSnapshot.LastWriteTime, await getScrapbookImageRevisionMetadataAsync(snapshotId, Key).ConfigureAwait(false), contentStream.ToArray().ToImmutableArray()));
            }
        }
    }

    async Task InitializeCurrentRevision(ReadOnlyMemory<byte> content)
    {
        try
        {
            ImageData = (await Ts4TranslucentJointPhotographicExpertsGroupImage.ConvertTranslucentJpegToPngAsync(content).ConfigureAwait(false)).ToImmutableArray();
            ImageType = ResourceFileType.Ts4TranslucentJointPhotographicExpertsGroupImage;
            return;
        }
        catch
        {
        }
        try
        {
            using var contentStream = new ReadOnlyMemoryOfByteStream(content);
            var imageFormat = await Image.DetectFormatAsync(contentStream).ConfigureAwait(false);
            if (imageFormat == PngFormat.Instance)
            {
                ImageData = content.ToImmutableArray();
                ImageType = ResourceFileType.PortableNetworkGraphic;
                return;
            }
            if (imageFormat == JpegFormat.Instance)
            {
                ImageData = content.ToImmutableArray();
                ImageType = ResourceFileType.JointPhotographicExpertsGroupImage;
                return;
            }
            logger.LogWarning("chronicle {NucleusId}-{Created} has image {Key} with unknown format", chronicle.NucleusId, chronicle.Created, Key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "chronicle {NucleusId}-{Created} has image {Key} with unknown format", chronicle.NucleusId, chronicle.Created, Key);
        }
    }
}
