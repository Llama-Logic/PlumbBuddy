namespace PlumbBuddy.Data;

public class ModFileResource
{
    [Key]
    public long Id { get; set; }

    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash? ModFileHash { get; set; }

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

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<TopologySnapshot>? TopologySnapshots { get; set; }
}
