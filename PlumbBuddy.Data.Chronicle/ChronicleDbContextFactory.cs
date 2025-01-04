namespace PlumbBuddy.Data.Chronicle;

public class ChronicleDbContextFactory :
    IDbContextFactory<ChronicleDbContext>
{
    public ChronicleDbContextFactory(FileInfo chronicle)
    {
        ArgumentNullException.ThrowIfNull(chronicle);
        this.chronicle = chronicle;
    }

    readonly FileInfo chronicle;

    public ChronicleDbContext CreateDbContext() =>
        new
        (
            new DbContextOptionsBuilder<ChronicleDbContext>()
                .UseSqlite($"Data Source={chronicle.FullName}").Options
        );
}
