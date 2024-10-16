namespace PlumbBuddy.Data;

public class HashResourceKey
{
    [Key]
    public long Id { get; set; }

    public long ModFileManifestId { get; set; }

    [ForeignKey(nameof(ModFileManifestId))]
    public ModFileManifest? ModFileManifest { get; set; }

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
}
