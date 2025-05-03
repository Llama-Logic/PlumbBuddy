namespace PlumbBuddy.Data;

[Index(nameof(Identifier), IsUnique = true)]
public class RequirementIdentifier
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Identifier { get; set; }

    public ICollection<RequiredMod> RequirementGroupMembers { get; } = [];
}
