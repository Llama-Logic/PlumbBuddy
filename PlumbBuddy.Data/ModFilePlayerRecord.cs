namespace PlumbBuddy.Data;

public class ModFilePlayerRecord
{
    [Key]
    public long Id { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? PersonalDate { get; set; }

    [InverseProperty(nameof(ModFilePlayerRecordPath.ModFilePlayerRecord))]
    public ICollection<ModFilePlayerRecordPath> ModFilePlayerRecordPaths { get; } = [];

    public ICollection<ModFileHash> ModFileHashes { get; } = [];
}
