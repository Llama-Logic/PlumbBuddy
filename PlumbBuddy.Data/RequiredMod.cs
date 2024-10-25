namespace PlumbBuddy.Data;

public class RequiredMod
{
    [Key]
    public long Id { get; set; }

    public long ModManfiestId { get; set; }

    [ForeignKey(nameof(ModManfiestId))]
    public ModFileManifest? ModFileManifest { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModCreator>? Creators { get; set; }

    public string? Version { get; set; }

    public Uri? Url { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifestHash>? Hashes { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFeature>? RequiredFeatures { get; set; }

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
