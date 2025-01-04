namespace PlumbBuddy.Data.Chronicle;

public class SavePackageResource
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(16, 16)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Key { get; set; }

    [Required]
    public required SavePackageResourceCompressionType CompressionType { get; set; }

    [Required]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] ContentZLib { get; set; }

    [Required]
    public required int ContentSize { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<SavePackageSnapshot>? Snapshots { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ResourceSnapshotDelta>? Deltas { get; set; }
}
