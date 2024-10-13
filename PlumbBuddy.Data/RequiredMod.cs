namespace PlumbBuddy.Data;

public class RequiredMod
{
    [Key]
    public long Id { get; set; }

    public long ModManfiestId { get; set; }

    [ForeignKey(nameof(ModManfiestId))]
    public ModManifest? ModManifest { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModCreator>? Creators { get; set; }

    public Version? Version { get; set; }

    public Uri? Url { get; set; }

    [NotMapped]
    public ResourceKey? ManifestKey
    {
        get =>
              ManifestKeyType is { } type && ManifestKeyGroup is { } group && ManifestKeyFullInstance is { } fullInstance
            ? new ResourceKey((ResourceType)unchecked((uint)type), unchecked((uint)group), unchecked((ulong)fullInstance))
            : default;
        set
        {
            if (value is ResourceKey key)
            {
                ManifestKeyType = unchecked((int)(uint)key.Type);
                ManifestKeyGroup = unchecked((int)key.Group);
                ManifestKeyFullInstance = unchecked((long)key.FullInstance);
            }
            else
            {
                ManifestKeyType = null;
                ManifestKeyGroup = null;
                ManifestKeyFullInstance = null;
            }
        }
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="ResourceType"/> (<see cref="uint"/>) type, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? ManifestKeyType { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> group, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? ManifestKeyGroup { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? ManifestKeyFullInstance { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModManifestHash>? Hashes { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFeature>? RequiredFeatures { get; set; }

    public long? RequirementIdentifierId { get; set; }

    [ForeignKey(nameof(RequirementIdentifierId))]
    public RequirementIdentifier? RequirementIdentifier { get; set; }

    public long? IgnoreIfHashAvailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfHashAvailableId))]
    public ModManifestHash? IgnoreIfHashAvailable { get; set; }

    public long? IgnoreIfHashUnavailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfHashUnavailableId))]
    public ModManifestHash? IgnoreIfHashUnavailable { get; set; }

    public long? IgnoreIfPackAvailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfPackAvailableId))]
    public PackCode? IgnoreIfPackAvailable { get; set; }

    public long? IgnoreIfPackUnavailableId { get; set; }

    [ForeignKey(nameof(IgnoreIfPackUnavailableId))]
    public PackCode? IgnoreIfPackUnavailable { get; set; }
}
