namespace PlumbBuddy.Archivist.Data;

public class ArchiveDbContext :
    DbContext
{
    public ArchiveDbContext() :
        base()
    {
    }

    public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) :
        base(options)
    {
    }

    public DbSet<Chronicle> Chronicles { get; set; }
    public DbSet<Snapshot> Snapshots { get; set; }

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

        modelBuilder.Entity<Chronicle>()
            .Property(e => e.Slot)
            .HasMaxLength(4)
            .IsFixedLength(true);
        modelBuilder.Entity<Chronicle>()
            .Property(e => e.FullInstance)
            .HasMaxLength(8)
            .IsFixedLength(true);
        modelBuilder.Entity<Snapshot>()
            .Property(e => e.PackageSha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<Snapshot>()
            .Property(e => e.RepositoryCommitHash)
            .HasMaxLength(20)
            .IsFixedLength(true);
    }
}
