namespace PlumbBuddy.Data;

public class ModFileManifestRepurposedLanguage(ModFileManifest modFileManifest)
{
    ModFileManifestRepurposedLanguage() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileManifestId { get; set; }

    [ForeignKey(nameof(ModFileManifestId))]
    public ModFileManifest ModFileManifest { get; set; } = modFileManifest;

    [Required]
    public required string ActualLocale { get; set; } = string.Empty;

    [Required]
    public required string GameLocale { get; set; } = string.Empty;
}
