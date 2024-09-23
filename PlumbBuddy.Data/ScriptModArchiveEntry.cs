namespace PlumbBuddy.Data;

[Index(nameof(ModFileHashId), nameof(FullName), IsUnique = true)]
public class ScriptModArchiveEntry
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash? ModFileHash { get; set; }

    public string? Comment { get; set; }

    [Required]
    public long CompressedLength { get; set; }

    [Required]
    public int SignedCrc32 { get; set; }

    [NotMapped]
    public uint Crc32
    {
        get => unchecked((uint)SignedCrc32);
        set => SignedCrc32 = unchecked((int)value);
    }

    [Required]
    public int ExternalAttributes { get; set; }

    [Required]
    public string FullName { get; set; }

    [Required]
    public bool IsEncrypted { get; set; }

    [Required]
    public DateTimeOffset LastWriteTime { get; set; }

    [Required]
    public long Length { get; set; }

    [Required]
    public string Name { get; set; }
}
