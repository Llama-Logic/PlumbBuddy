namespace PlumbBuddy.Data;

[Index(nameof(Path), nameof(Creation), nameof(LastWrite), nameof(Size))]
[Index(nameof(Path))]
public class ModFile
{
    [Key]
    public long Id { get; set; }

    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash? ModFileHash { get; set; }

    public long? ModManifestId { get; set; }

    [ForeignKey(nameof(ModManifestId))]
    public ModManifest? ModManifest { get; set; }

    public string? Path { get; set; }

    public DateTimeOffset? Creation { get; set; }

    public DateTimeOffset? LastWrite { get; set; }

    public long? Size { get; set; }

    public ModsDirectoryFileType FileType { get; set; }

    public DateTimeOffset? AbsenceNoticed { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileResource>? Resources { get; set; }
}
