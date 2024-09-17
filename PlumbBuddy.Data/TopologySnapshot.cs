namespace PlumbBuddy.Data;

public class TopologySnapshot
{
    [Key]
    public long Id { get; set; }

    [Required]
    public DateTimeOffset Taken { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileResource>? Resources { get; set; }
}
