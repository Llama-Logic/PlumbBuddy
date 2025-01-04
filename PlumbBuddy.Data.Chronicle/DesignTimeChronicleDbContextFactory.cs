namespace PlumbBuddy.Data.Chronicle;

public class DesignTimeChronicleDbContextFactory :
    IDesignTimeDbContextFactory<ChronicleDbContext>
{
    public ChronicleDbContext CreateDbContext(string[] args) =>
        new
        (
            new DbContextOptionsBuilder<ChronicleDbContext>()
                .UseSqlite("Data Source=Chronicle.sqlite").Options
        );
}
