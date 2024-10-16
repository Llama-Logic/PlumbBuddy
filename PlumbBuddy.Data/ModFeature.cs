namespace PlumbBuddy.Data;

[Index(nameof(Name), IsUnique = true)]
public class ModFeature
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifest>? SpecifiedByModFileManifests { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? SpecifiedByRequiredMods { get; set; }
}
