namespace PlumbBuddy.Data.Chronicle;

public class ChronicleDbContext :
    DbContext
{
    public ChronicleDbContext() :
        base()
    {
    }

    public ChronicleDbContext(DbContextOptions<ChronicleDbContext> options) :
        base(options)
    {
    }

    public DbSet<ChroniclePropertySet> ChroniclePropertySets { get; set; }
    public DbSet<KnownSavePackageHash> KnownSavePackageHashes { get; set; }
    public DbSet<ResourceSnapshotDelta> ResourceSnapshotDeltas { get; set; }
    public DbSet<SavePackageResource> SavePackageResources { get; set; }
    public DbSet<SavePackageSnapshot> SavePackageSnapshots { get; set; }
    public DbSet<SnapshotModFile> SnapshotModFiles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new SQLiteWalConnectionInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ChroniclePropertySet>()
            .Property(e => e.FullInstance)
            .HasMaxLength(8)
            .IsFixedLength(true);
        modelBuilder.Entity<ChroniclePropertySet>()
            .Property(e => e.BasisFullInstance)
            .HasMaxLength(8)
            .IsFixedLength(true);
        modelBuilder.Entity<ChroniclePropertySet>()
            .Property(e => e.BasisOriginalPackageSha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<KnownSavePackageHash>()
            .Property(e => e.Sha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<SavePackageResource>()
            .Property(e => e.Key)
            .HasMaxLength(16)
            .IsFixedLength(true);
        modelBuilder.Entity<SnapshotModFile>()
            .Property(e => e.Sha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
    }
}
