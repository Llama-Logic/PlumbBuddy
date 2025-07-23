namespace PlumbBuddy.Data;

[Index(nameof(Path), IsUnique = true)]
[Index(nameof(Path), nameof(Creation), nameof(LastWrite), nameof(Size))]
public class GameResourcePackage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Path { get; set; }

    public DateTimeOffset Creation { get; set; }

    public DateTimeOffset LastWrite { get; set; }

    public long Size { get; set; }

    public bool IsDelta { get; set; }

    public ICollection<GameResourcePackageResource> Resources { get; } = [];
}
