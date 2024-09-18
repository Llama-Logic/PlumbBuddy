namespace PlumbBuddy.Data;

[Index(nameof(Code))]
public class PackCode
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Code { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModManifest>? RequiredByMods { get; set; }
}
