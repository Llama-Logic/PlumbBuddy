namespace PlumbBuddy.Data;

public class RecommendedPack(ModFileManifest modFileManifest, PackCode packCode)
{
    RecommendedPack() :
        this(null!, null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileManfiestId { get; set; }

    [ForeignKey(nameof(ModFileManfiestId))]
    public ModFileManifest ModFileManifest { get; set; } = modFileManifest;

    public long PackCodeId { get; set; }

    [ForeignKey(nameof(PackCodeId))]
    public PackCode PackCode { get; set; } = packCode;

    [Required]
    public string Reason { get; set; } = string.Empty;
}
