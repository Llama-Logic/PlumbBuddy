namespace PlumbBuddy.Data;

[Index(nameof(Code), IsUnique = true)]
public class ElectronicArtsPromoCode
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Code { get; set; }

    public ICollection<ModFileManifest> ReferencingModFileManifests { get; } = [];
}
