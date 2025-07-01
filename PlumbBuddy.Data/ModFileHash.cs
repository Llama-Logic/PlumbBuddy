namespace PlumbBuddy.Data;

[Index(nameof(Sha256), IsUnique = true)]
public class ModFileHash
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    public ICollection<ModFile> ModFiles { get; } = [];

    [Required]
    public bool ResourcesAndManifestsCataloged { get; set; }

    public ICollection<ModFileResource> Resources { get; } = [];

    public ICollection<ScriptModArchiveEntry> ScriptModArchiveEntries { get; } = [];

    public ICollection<ModFileManifest> ModFileManifests { get; } = [];

    public required bool IsCorrupt { get; set; }

    public bool StringTablesCataloged { get; set; }
}
