namespace PlumbBuddy.Data;

[Index(nameof(Sha256), IsUnique = true)]
public class ModFileManifestHash
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Length(32, 32)]
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public required byte[] Sha256 { get; set; }

    [InverseProperty(nameof(ModFileManifest.CalculatedModFileManifestHash))]
    public ICollection<ModFileManifest> ManifestsByCalculation { get; } = [];

    [InverseProperty(nameof(ModFileManifest.InscribedModFileManifestHash))]
    public ICollection<ModFileManifest> ManifestsByInscription { get; } = [];

    [InverseProperty(nameof(ModFileManifest.SubsumedHashes))]
    public ICollection<ModFileManifest> ManifestsBySubsumption { get; } = [];

    [InverseProperty(nameof(RequiredMod.Hashes))]
    public ICollection<RequiredMod> Dependents { get; } = [];

    [InverseProperty(nameof(RequiredMod.IgnoreIfHashAvailable))]
    public ICollection<RequiredMod> DisqualifyingByPresence { get; } = [];

    [InverseProperty(nameof(RequiredMod.IgnoreIfHashUnavailable))]
    public ICollection<RequiredMod> DisqualifyingByAbsence { get; } = [];
}
