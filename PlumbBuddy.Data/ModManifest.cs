namespace PlumbBuddy.Data;

public class ModManifest
{
    [Key]
    public long Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModCreator>? Creators { get; set; }

    public Version? Version { get; set; }

    public Uri? Url { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<ModFileHash>? SubsumedFiles { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<PackCode>? RequiredPacks { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<RequiredMod>? RequiredMods { get; set; }

    [SuppressMessage("Usage", "CA2227: Collection properties should be read only")]
    public ICollection<IntentionalOverride>? IntentionalOverrides { get; set; }
}
