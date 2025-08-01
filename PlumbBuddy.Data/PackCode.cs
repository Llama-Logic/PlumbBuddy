namespace PlumbBuddy.Data;

[Index(nameof(Code), IsUnique = true)]
public class PackCode
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Code { get; set; }

    public ICollection<RecommendedPack> RecommendedPacks { get; } = [];

    [InverseProperty(nameof(ModFileManifest.RequiredPacks))]
    public ICollection<ModFileManifest> RequiredByMods { get; } = [];

    [InverseProperty(nameof(ModFileManifest.IncompatiblePacks))]
    public ICollection<ModFileManifest> IncompatibleWithMods { get; } = [];

    [InverseProperty(nameof(RequiredMod.IgnoreIfPackAvailable))]
    public ICollection<RequiredMod> DisqualifyingByPresence { get; } = [];

    [InverseProperty(nameof(RequiredMod.IgnoreIfPackUnavailable))]
    public ICollection<RequiredMod> DisqualifyingByAbsence { get; } = [];
}
