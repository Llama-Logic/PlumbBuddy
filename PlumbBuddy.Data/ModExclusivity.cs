namespace PlumbBuddy.Data;

[Index("Name", IsUnique = true)]
public class ModExclusivity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModManifest>? SpecifiedByModManifests { get; set; }
}
