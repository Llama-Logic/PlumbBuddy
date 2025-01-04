namespace PlumbBuddy.Data.Chronicle;

[Index(nameof(Sha256), IsUnique = true)]
public class KnownSavePackageHash
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] Sha256 { get; set; } = [];
}
