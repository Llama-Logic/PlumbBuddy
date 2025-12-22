namespace PlumbBuddy.Services.ScriptApi;

public class GetScreenshotDetailsResponseMessage :
    HostMessageBase
{
    public FileAttributes Attributes { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime CreationTimeUtc { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime LastAccessTimeUtc { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    public uint MetadataVersion { get; set; }
    public required string Name { get; init; }
    public long Size { get; set; }
    public UnixFileMode UnixFileMode { get; set; }
}
