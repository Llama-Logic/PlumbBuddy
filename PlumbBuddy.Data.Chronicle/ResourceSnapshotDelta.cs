namespace PlumbBuddy.Data.Chronicle;

public class ResourceSnapshotDelta
{
    [Key]
    public long Id { get; set; }

    public long SavePackageSnapshotId { get; set; }

    [ForeignKey(nameof(SavePackageSnapshotId))]
    public SavePackageSnapshot? SavePackageSnapshot { get; set; }

    public long SavePackageResourceId { get; set; }

    [ForeignKey(nameof(SavePackageResourceId))]
    public SavePackageResource? SavePackageResource { get; set; }

    [Required]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] PatchZLib { get; set; }

    [Required]
    public required int PatchSize { get; set; }
}