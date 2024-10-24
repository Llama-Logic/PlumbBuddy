namespace PlumbBuddy.Data;

[Index(nameof(Code), IsUnique = true)]
public class ElectronicArtsPromoCode
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Code { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileManifest>? ReferencingModFileManifests { get; set; }
}
