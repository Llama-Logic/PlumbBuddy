namespace PlumbBuddy.Data;

[Index(nameof(Path), nameof(Creation), nameof(LastWrite), nameof(Size))]
[Index(nameof(Path), IsUnique = true)]
public class GameStringsPackage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Path { get; set; }

    public DateTimeOffset Creation { get; set; }

    public DateTimeOffset LastWrite { get; set; }

    public long Size { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    public bool IsDelta { get; set; }

    public ICollection<GameStringsPackageResource> Resources { get; } = [];
}
