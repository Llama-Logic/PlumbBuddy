namespace PlumbBuddy.Data;

[Index(nameof(Name), IsUnique = true)]
public class ModCreator
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public ICollection<ModFileManifest> AttributedMods { get; } = [];

    public ICollection<RequiredMod> AttributedRequiredMods { get; } = [];
}
