namespace PlumbBuddy.Data;

public class TopologySnapshot
{
    [Key]
    public long Id { get; set; }

    [Required]
    public DateTimeOffset Taken { get; set; }

    public ICollection<ModFileResource> Resources { get; } = [];
}
