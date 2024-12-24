namespace PlumbBuddy.Archivist.Data;

[Index(nameof(Slot), nameof(FullInstance), IsUnique = true)]
public class Chronicle
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(16, 16)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Guid { get; set; }

    [Required]
    [Length(4, 4)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Slot { get; set; }

    [Required]
    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] FullInstance { get; set; }

    public long? BranchedFromSnapshotId { get; set; }

    [ForeignKey(nameof(BranchedFromSnapshotId))]
    public Snapshot? BranchedFromSnapshot { get; set; }
}
