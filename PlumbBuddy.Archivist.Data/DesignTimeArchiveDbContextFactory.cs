namespace PlumbBuddy.Archivist.Data;

public class DesignTimeArchiveDbContextFactory :
    IDesignTimeDbContextFactory<ArchiveDbContext>
{
    public ArchiveDbContext CreateDbContext(string[] args) =>
        new
        (
            new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite("Data Source=Archive.sqlite").Options
        );
}
