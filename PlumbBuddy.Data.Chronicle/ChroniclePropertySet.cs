namespace PlumbBuddy.Data.Chronicle;

public class ChroniclePropertySet
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] NucleusId { get; set; } = [];

    [Required]
    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] Created { get; set; } = [];

    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[]? BasisNucleusId { get; set; }

    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[]? BasisCreated { get; set; }

    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[]? BasisOriginalPackageSha256 { get; set; }

    public string? GameNameOverride { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Required]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] Thumbnail { get; set; } = [];
}
