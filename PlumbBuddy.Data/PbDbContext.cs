namespace PlumbBuddy.Data;

public class PbDbContext :
    DbContext
{
    public PbDbContext() :
        base()
    {
    }

    public PbDbContext(DbContextOptions<PbDbContext> options) :
        base(options)
    {
    }

    public DbSet<ElectronicArtsPromoCode> ElectronicArtsPromoCodes { get; set; }
    public DbSet<FileOfInterest> FilesOfInterest { get; set; }
    public DbSet<ModCreator> ModCreators { get; set; }
    public DbSet<ModExclusivity> ModExclusivities { get; set; }
    public DbSet<ModFeature> ModFeatures { get; set; }
    public DbSet<ModFileHash> ModFileHashes { get; set; }
    public DbSet<ModFileResource> ModFileResources { get; set; }
    public DbSet<ModFile> ModFiles { get; set; }
    public DbSet<ModFileManifestHash> ModFileManifestHashes { get; set; }
    public DbSet<ModFileManifestResourceKey> ModFileManifestResourceKeys { get; set; }
    public DbSet<ModFileManifest> ModFileManifests { get; set; }
    public DbSet<PackCode> PackCodes { get; set; }
    public DbSet<RequiredMod> RequiredMods { get; set; }
    public DbSet<RequirementIdentifier> RequirementIdentifiers { get; set; }
    public DbSet<ScriptModArchiveEntry> ScriptModArchiveEntries { get; set; }
    public DbSet<TopologySnapshot> TopologySnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var nullableUriValueConverter = new ValueConverter<Uri?, string?>
        (
            maybeNullUri =>
                maybeNullUri == null
                ? null
                : maybeNullUri.AbsoluteUri,
            maybeNullUriStr =>
                maybeNullUriStr == null
                ? null
                : new Uri(maybeNullUriStr, UriKind.Absolute)
        );

        modelBuilder.Entity<ModFileHash>()
            .Property(e => e.Sha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<ModFileManifestHash>()
            .Property(e => e.Sha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<ModFileManifest>()
            .Property(e => e.Url)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<RequiredMod>()
            .Property(e => e.Url)
            .HasConversion(nullableUriValueConverter);
    }
}
