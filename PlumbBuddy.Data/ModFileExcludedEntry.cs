namespace PlumbBuddy.Data;

public class ModFileExcludedEntry(ModFileManifest modFileManifest)
{
    ModFileExcludedEntry() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileManifestId { get; set; }

    [ForeignKey(nameof(ModFileManifestId))]
    public ModFileManifest ModFileManifest { get; set; } = modFileManifest;

    [Required]
    public required string Name { get; set; }
}
