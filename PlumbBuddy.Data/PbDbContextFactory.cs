namespace PlumbBuddy.Data;

public class PbDbContextFactory :
    IDesignTimeDbContextFactory<PbDbContext>
{
    public PbDbContext CreateDbContext(string[] args) =>
        new
        (
            new DbContextOptionsBuilder<PbDbContext>()
                .UseSqlite("Data Source=PlumbBuddy.sqlite").Options
        );
}