namespace PlumbBuddy.Data;

[Index(nameof(Identifier), IsUnique = true)]
public class RequirementIdentifier
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Identifier { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? RequirementGroupMembers { get; set; }
}
