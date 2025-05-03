namespace PlumbBuddy.Data;

[Index(nameof(Name), IsUnique = true)]
public class ModFeature
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public ICollection<ModFileManifest> SpecifiedByModFileManifests { get; } = [];

    public ICollection<RequiredMod> SpecifiedByRequiredMods { get; } = [];
}
