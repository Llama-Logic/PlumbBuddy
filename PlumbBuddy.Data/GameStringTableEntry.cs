namespace PlumbBuddy.Data;

[Index(nameof(GameResourcePackageResourceId), nameof(SignedKey), IsUnique = true)]
[Index(nameof(GameResourcePackageResourceId))]
public class GameStringTableEntry(GameResourcePackageResource gameResourcePackageResource)
{
    GameStringTableEntry() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long GameResourcePackageResourceId { get; set; }

    [ForeignKey(nameof(GameResourcePackageResourceId))]
    public GameResourcePackageResource GameStringsPackageResource { get; set; } = gameResourcePackageResource;

    [NotMapped]
    public uint Key
    {
        get => unchecked((uint)SignedKey);
        set => SignedKey = unchecked((int)value);
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> type used by LOCKEYs, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    [Required]
    public int SignedKey { get; set; }
}
