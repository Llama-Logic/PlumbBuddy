namespace PlumbBuddy.Data;

public class IntentionalOverride
{
    [Key]
    public long Id { get; set; }

    public long ModManfiestId { get; set; }

    [ForeignKey(nameof(ModManfiestId))]
    public ModManifest? ModManifest { get; set; }

    [NotMapped]
    public ResourceKey Key
    {
        get => new((ResourceType)unchecked((uint)KeyType), unchecked((uint)KeyGroup), unchecked((ulong)KeyFullInstance));
        set
        {
            KeyType = unchecked((int)(uint)value.Type);
            KeyGroup = unchecked((int)value.Group);
            KeyFullInstance = unchecked((long)value.FullInstance);
        }
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="ResourceType"/> (<see cref="uint"/>) type, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    [Required]
    public int KeyType { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> group, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    [Required]
    public int KeyGroup { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    [Required]
    public long KeyFullInstance { get; set; }

    public string? Name { get; set; }

    public string? ModName { get; set; }

    public Version? ModVersion { get; set; }

    [NotMapped]
    public ResourceKey? ModManifestKey
    {
        get =>
              ModManifestKeyType is { } type && ModManifestKeyGroup is { } group && ModManifestKeyFullInstance is { } fullInstance
            ? new ResourceKey((ResourceType)unchecked((uint)type), unchecked((uint)group), unchecked((ulong)fullInstance))
            : default;
        set
        {
            if (value is ResourceKey key)
            {
                ModManifestKeyType = unchecked((int)(uint)key.Type);
                ModManifestKeyGroup = unchecked((int)key.Group);
                ModManifestKeyFullInstance = unchecked((long)key.FullInstance);
            }
            else
            {
                ModManifestKeyType = null;
                ModManifestKeyGroup = null;
                ModManifestKeyFullInstance = null;
            }
        }
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="ResourceType"/> (<see cref="uint"/>) type, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? ModManifestKeyType { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> group, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? ModManifestKeyGroup { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? ModManifestKeyFullInstance { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileHash>? ModFiles { get; set; }
}
