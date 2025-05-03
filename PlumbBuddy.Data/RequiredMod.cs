namespace PlumbBuddy.Data;

public class RequiredMod(ModFileManifest modFileManifest)
{
    RequiredMod() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileManfiestId { get; set; }

    [ForeignKey(nameof(ModFileManfiestId))]
    public ModFileManifest ModFileManifest { get; set; } = modFileManifest;

    [Required]
    public required string Name { get; set; }

    public ICollection<ModCreator> Creators { get; } = [];

    public string? Version { get; set; }

    public Uri? Url { get; set; }

    public ICollection<ModFileManifestHash> Hashes { get; } = [];

    public ICollection<ModFeature> RequiredFeatures { get; } = [];

    public long? RequirementIdentifierId { get; set; }

    [ForeignKey(nameof(RequirementIdentifierId))]
    public RequirementIdentifier? RequirementIdentifier { get; set; }

    public long? IgnoreIfHashAvailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfHashAvailableId))]
    public ModFileManifestHash? IgnoreIfHashAvailable { get; set; }

    public long? IgnoreIfHashUnavailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfHashUnavailableId))]
    public ModFileManifestHash? IgnoreIfHashUnavailable { get; set; }

    public long? IgnoreIfPackAvailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfPackAvailableId))]
    public PackCode? IgnoreIfPackAvailable { get; set; }

    public long? IgnoreIfPackUnavailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfPackUnavailableId))]
    public PackCode? IgnoreIfPackUnavailable { get; set; }
}
