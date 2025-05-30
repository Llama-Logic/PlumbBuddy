namespace PlumbBuddy.Data;

[Index(nameof(Path), nameof(Creation), nameof(LastWrite), nameof(Size))]
[Index(nameof(Path), IsUnique = true)]
public class ModFile(ModFileHash modFileHash)
{
    ModFile() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash ModFileHash { get; set; } = modFileHash;

    [Required]
    public required string Path { get; set; }

    public DateTimeOffset Creation { get; set; }

    public DateTimeOffset LastWrite { get; set; }

    public long Size { get; set; }

    public ModsDirectoryFileType FileType { get; set; }
}
