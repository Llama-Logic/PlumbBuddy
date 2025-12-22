namespace PlumbBuddy.Data.Chronicle;

public class SavePackageSnapshotDefect(SavePackageSnapshot savePackageSnapshot, string description)
{
    SavePackageSnapshotDefect() :
        this(null!, null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long SavePackageSnapshotId { get; set; }

    [ForeignKey(nameof(SavePackageSnapshotId))]
    public SavePackageSnapshot SavePackageSnapshot { get; set; } = savePackageSnapshot;

    public SavePackageSnapshotDefectType SavePackageSnapshotDefectType { get; set; }

    public string Description { get; set; } = description;
}
