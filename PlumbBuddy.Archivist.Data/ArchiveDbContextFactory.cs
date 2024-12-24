namespace PlumbBuddy.Archivist.Data;

public class ArchiveDbContextFactory :
    IDbContextFactory<ArchiveDbContext>
{
    public ArchiveDbContextFactory(DirectoryInfo archiveFolder)
    {
        ArgumentNullException.ThrowIfNull(archiveFolder);
        this.archiveFolder = archiveFolder;
    }

    readonly DirectoryInfo archiveFolder;

    public ArchiveDbContext CreateDbContext() =>
        new
        (
            new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite($"Data Source={Path.Combine(archiveFolder.FullName, "Archive.sqlite")}").Options
        );
}
