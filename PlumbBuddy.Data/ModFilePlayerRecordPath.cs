namespace PlumbBuddy.Data;

[Index(nameof(ModFilePlayerRecordId), nameof(Path), IsUnique = true)]
public class ModFilePlayerRecordPath(ModFilePlayerRecord modFilePlayerRecord)
{
    ModFilePlayerRecordPath() :
        this(null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFilePlayerRecordId { get; set; }

    [ForeignKey(nameof(ModFilePlayerRecordId))]
    public ModFilePlayerRecord ModFilePlayerRecord { get; set; } = modFilePlayerRecord;

    [Required]
    public required string Path { get; set; }
}
