namespace PlumbBuddy.Data.Chronicle;

public class SavePackageSnapshot
{
    [Key]
    public long Id { get; set; }

    public long? OriginalSavePackageHashId { get; set; }

    [ForeignKey(nameof(OriginalSavePackageHashId))]
    public KnownSavePackageHash? OriginalSavePackageHash { get; set; }

    public long? EnhancedSavePackageHashId { get; set; }

    [ForeignKey(nameof(EnhancedSavePackageHashId))]
    public KnownSavePackageHash? EnhancedSavePackageHash { get; set; }

    [Required]
    public DateTimeOffset LastWriteTime { get; set; }

    public ICollection<SavePackageResource> Resources { get; } = [];

    public ICollection<ResourceSnapshotDelta> Deltas { get; } = [];

    public ICollection<SnapshotModFile> ModFiles { get; } = [];

    public ICollection<SavePackageSnapshotDefect> Defects { get; } = [];

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Required]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] Thumbnail { get; set; } = [];

    public string? ActiveHouseholdName { get; set; }

    public string? LastPlayedLotName { get; set; }

    public string? LastPlayedWorldName { get; set; }

    [Required]
    public bool WasLive { get; set; }
}
