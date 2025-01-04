namespace PlumbBuddy.Data.Chronicle;

[Index(nameof(Path), nameof(LastWriteTime), nameof(Size), nameof(Sha256), IsUnique = true)]
public class SnapshotModFile
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Path { get; set; }

    [Required]
    public required DateTimeOffset LastWriteTime { get; set; }

    [Required]
    public required long Size { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<SavePackageSnapshot>? Snapshots { get; set; }
}
