namespace PlumbBuddy.Data.Chronicle;

public class ChroniclePropertySet
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] FullInstance { get; set; } = [];

    [Length(8, 8)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[]? BasisFullInstance { get; set; }

    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[]? BasisOriginalPackageSha256 { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Required]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public byte[] Thumbnail { get; set; } = [];
}
