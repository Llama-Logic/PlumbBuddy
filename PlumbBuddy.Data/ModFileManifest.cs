namespace PlumbBuddy.Data;

public class ModFileManifest
{
    [Key]
    public long Id { get; set; }

    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash? ModFileHash { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModCreator>? Creators { get; set; }

    public string? Version { get; set; }

    public Uri? Url { get; set; }

    public long InscribedModFileManifestHashId { get; set; }

    [ForeignKey(nameof(InscribedModFileManifestHashId))]
    public ModFileManifestHash? InscribedModFileManifestHash { get; set; }

    public ModFileManifestResourceHashStrategy? ResourceHashStrategy { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<HashResourceKey>? HashResourceKeys { get; set; }

    public long CalculatedModFileManifestHashId { get; set; }

    [ForeignKey(nameof(CalculatedModFileManifestHashId))]
    public ModFileManifestHash? CalculatedModFileHash { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifestHash>? SubsumedHashes { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFeature>? Features { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModExclusivity>? Exclusivities { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<PackCode>? RequiredPacks { get; set; }

    public long? ElectronicArtsPromoCodeId { get; set; }

    [ForeignKey(nameof(ElectronicArtsPromoCodeId))]
    public ElectronicArtsPromoCode? ElectronicArtsPromoCode { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<PackCode>? IncompatiblePacks { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? RequiredMods { get; set; }

    public string? TuningName { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? TuningFullInstance { get; set; }

    [NotMapped]
    public ResourceKey? Key
    {
        get =>
            KeyType is { } keyType && KeyGroup is { } keyGroup && KeyFullInstance is { } keyFullInstance
                ? new((ResourceType)unchecked((uint)keyType), unchecked((uint)keyGroup), unchecked((ulong)keyFullInstance))
                : null;
        set
        {
            if (value is { } key)
            {
                KeyType = unchecked((int)(uint)key.Type);
                KeyGroup = unchecked((int)key.Group);
                KeyFullInstance = unchecked((long)key.FullInstance);
            }
            else
            {
                KeyType = null;
                KeyGroup = null;
                KeyFullInstance = null;
            }
        }
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="ResourceType"/> (<see cref="uint"/>) type, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? KeyType { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> group, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? KeyGroup { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? KeyFullInstance { get; set; }
}
