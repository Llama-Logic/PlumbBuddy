namespace PlumbBuddy.Data;

[Index(nameof(Code))]
public class PackCode
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Code { get; set; }

    [InverseProperty(nameof(ModFileManifest.RequiredPacks))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifest>? RequiredByMods { get; set; }

    [InverseProperty(nameof(ModFileManifest.IncompatiblePacks))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifest>? IncompatibleWithMods { get; set; }

    [InverseProperty(nameof(RequiredMod.IgnoreIfPackAvailable))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? DisqualifyingByPresence { get; set; }

    [InverseProperty(nameof(RequiredMod.IgnoreIfPackUnavailable))]
    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? DisqualifyingByAbsence { get; set; }
}
