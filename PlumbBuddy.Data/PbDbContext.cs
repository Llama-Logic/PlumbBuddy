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
    public DbSet<GameResourcePackage> GameResourcePackages { get; set; }
    public DbSet<GameResourcePackageResource> GameResourcePackageResources { get; set; }
    public DbSet<GameStringTableEntry> GameStringTableEntries { get; set; }
    public DbSet<ModCreator> ModCreators { get; set; }
    public DbSet<ModExclusivity> ModExclusivities { get; set; }
    public DbSet<ModFeature> ModFeatures { get; set; }
    public DbSet<ModFileExcludedEntry> ModFileExcludedEntries { get; set; }
    public DbSet<ModFileHash> ModFileHashes { get; set; }
    public DbSet<ModFileResource> ModFileResources { get; set; }
    public DbSet<ModFile> ModFiles { get; set; }
    public DbSet<ModFileManifestHash> ModFileManifestHashes { get; set; }
    public DbSet<ModFileManifestResourceKey> ModFileManifestResourceKeys { get; set; }
    public DbSet<ModFileManifest> ModFileManifests { get; set; }
    public DbSet<ModFileManifestRepurposedLanguage> ModFileManifestRepurposedLanguages { get; set; }
    public DbSet<ModFilePlayerRecord> ModFilePlayerRecords { get; set; }
    public DbSet<ModFilePlayerRecordPath> ModFilePlayerRecordPaths { get; set; }
    public DbSet<ModFileStringTableEntry> ModFileStringTableEntries { get; set; }
    public DbSet<ModHoundReport> ModHoundReports { get; set; }
    public DbSet<ModHoundReportIncompatibilityRecord> ModHoundReportIncompatibilityRecords { get; set; }
    public DbSet<ModHoundReportIncompatibilityRecordPart> ModHoundReportIncompatibilityRecordParts { get; set; }
    public DbSet<ModHoundReportMissingRequirementsRecord> ModHoundReportMissingRequirementsRecords { get; set; }
    public DbSet<ModHoundReportMissingRequirementsRecordDependency> ModHoundReportMissingRequirementsRecordDependencies { get; set; }
    public DbSet<ModHoundReportMissingRequirementsRecordDependent> ModHoundReportMissingRequirementsRecordDependents { get; set; }
    public DbSet<ModHoundReportNotTrackedRecord> ModHoundReportNotTrackedRecords { get; set; }
    public DbSet<ModHoundReportRecord> ModHoundReportRecords { get; set; }
    public DbSet<PackCode> PackCodes { get; set; }
    public DbSet<RecommendedPack> RecommendedPacks { get; set; }
    public DbSet<RequiredMod> RequiredMods { get; set; }
    public DbSet<RequirementIdentifier> RequirementIdentifiers { get; set; }
    public DbSet<ScriptModArchiveEntry> ScriptModArchiveEntries { get; set; }
    public DbSet<TopologySnapshot> TopologySnapshots { get; set; }
    public DbSet<ModFileManifestTranslator> ModFileManifestTranslators { get; set; }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors
        (
            new SQLiteWalConnectionInterceptor(),
            new SQLiteBusyTimeoutConnectionInterceptor(TimeSpan.FromSeconds(5))
        );
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var dtoConverter = new ValueConverter<DateTimeOffset, long>
        (
            v => v.ToUnixTimeSeconds(),
            v => DateTimeOffset.FromUnixTimeSeconds(v).ToLocalTime()
        );

        var nullableDtoConverter = new ValueConverter<DateTimeOffset?, long?>
        (
            v => v.HasValue ? v.Value.ToUnixTimeSeconds() : null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeSeconds(v.Value).ToLocalTime() : null
        );

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

        modelBuilder.Entity<GameResourcePackage>()
            .Property(e => e.Creation)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<GameResourcePackage>()
            .Property(e => e.LastWrite)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModFile>()
            .Property(e => e.Creation)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModFile>()
            .Property(e => e.LastWrite)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModFile>()
            .Property(e => e.FoundAbsent)
            .HasConversion(nullableDtoConverter);
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
        modelBuilder.Entity<ModFileManifest>()
            .Property(e => e.ContactUrl)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<ModFileManifest>()
            .Property(e => e.TranslationSubmissionUrl)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<ModFileManifest>()
            .Property(e => e.FundingUrl)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<ModFilePlayerRecord>()
            .Property(e => e.PersonalDate)
            .HasConversion(nullableDtoConverter);
        modelBuilder.Entity<ModHoundReport>()
            .Property(e => e.RequestSha256)
            .HasMaxLength(32)
            .IsFixedLength(true);
        modelBuilder.Entity<ModHoundReport>()
            .Property(e => e.Retrieved)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModHoundReport>()
            .Property(e => e.LastEditedAtAny)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModHoundReportMissingRequirementsRecordDependency>()
            .Property(e => e.ModLinkOrIndexHref)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<ModHoundReportNotTrackedRecord>()
            .Property(e => e.FileDate)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModHoundReportRecord>()
            .Property(e => e.LastUpdateDate)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModHoundReportRecord>()
            .Property(e => e.DateOfInstalledFile)
            .HasConversion(dtoConverter);
        modelBuilder.Entity<ModHoundReportRecord>()
            .Property(e => e.ModLinkOrIndexHref)
            .HasConversion(nullableUriValueConverter);
        modelBuilder.Entity<RequiredMod>()
            .Property(e => e.Url)
            .HasConversion(nullableUriValueConverter);
    }
}
