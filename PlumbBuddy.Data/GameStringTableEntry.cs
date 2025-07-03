namespace PlumbBuddy.Data;

[Index(nameof(GameStringsPackageResourceId), nameof(SignedKey), IsUnique = true)]
public class GameStringTableEntry(GameStringsPackageResource gameStringsPackageResource)
{
    GameStringTableEntry() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long GameStringsPackageResourceId { get; set; }

    [ForeignKey(nameof(GameStringsPackageResourceId))]
    public GameStringsPackageResource GameStringsPackageResource { get; set; } = gameStringsPackageResource;

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
