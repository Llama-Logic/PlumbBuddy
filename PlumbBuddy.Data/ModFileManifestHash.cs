namespace PlumbBuddy.Data;

public class ModFileManifestHash
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? Dependents { get; set; }

    [InverseProperty(nameof(RequiredMod.IgnoreIfHashAvailable))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? DisqualifyingByPresence { get; set; }

    [InverseProperty(nameof(RequiredMod.IgnoreIfHashUnavailable))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? DisqualifyingByAbsence { get; set; }
}
