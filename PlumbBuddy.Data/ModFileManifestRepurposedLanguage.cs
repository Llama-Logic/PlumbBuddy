namespace PlumbBuddy.Data;

public class ModFileManifestRepurposedLanguage
{
    [Key]
    public long Id { get; set; }

    public long ModFileManifestId { get; set; }

    [ForeignKey(nameof(ModFileManifestId))]
    public ModFileManifest? ModFileManifest { get; set; }

    [Required]
    public required string From { get; set; } = string.Empty;

    [Required]
    public required string To { get; set; } = string.Empty;
}
