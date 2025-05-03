namespace PlumbBuddy.Data;

[Index("Name", IsUnique = true)]
public class ModExclusivity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public ICollection<ModFileManifest> SpecifiedByModFileManifests { get; } = [];
}
