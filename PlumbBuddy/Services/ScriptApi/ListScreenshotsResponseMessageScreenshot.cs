namespace PlumbBuddy.Services.ScriptApi;

public class ListScreenshotsResponseMessageScreenshot
{
    public FileAttributes Attributes { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime CreationTimeUtc { get; init; }
    public DateTime LastAccessTime { get; init; }
    public DateTime LastAccessTimeUtc { get; init; }
    public DateTime LastWriteTime { get; init; }
    public DateTime LastWriteTimeUtc { get; init; }
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public uint MetadataVersion { get; set; }
    public required string Name { get; init; }
    public long Size { get; init; }
    public UnixFileMode UnixFileMode { get; init; }
}
