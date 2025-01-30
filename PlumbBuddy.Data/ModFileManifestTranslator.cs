namespace PlumbBuddy.Data;

public class ModFileManifestTranslator
{
    [Key]
    public long Id { get; set; }

    public long ModFileManifestId { get; set; }

    [ForeignKey(nameof(ModFileManifestId))]
    public ModFileManifest? ModFileManifest { get; set; }

    [Required]
    public required string Language { get; set; } = string.Empty;

    [Required]
    public required string Name { get; set; } = string.Empty;
}
