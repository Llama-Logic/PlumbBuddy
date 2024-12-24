namespace PlumbBuddy.Archivist.Data;

public class Snapshot
{
    [Key]
    public long Id { get; set; }

    public long ChronicleId { get; set; }

    [ForeignKey(nameof(ChronicleId))]
    public Chronicle? Chronicle { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] PackageSha256 { get; set; }

    [Required]
    [Length(20, 20)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] RepositoryCommitHash { get; set; }
}
