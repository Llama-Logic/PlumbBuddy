namespace PlumbBuddy.Data;

[Index(nameof(Name))]
public class ModCreator
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifest>? AttributedMods { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? AttributedRequiredMods { get; set; }
}
