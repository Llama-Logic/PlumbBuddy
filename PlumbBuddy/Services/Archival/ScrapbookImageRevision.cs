namespace PlumbBuddy.Services.Archival;

public sealed record ScrapbookImageRevision(long SnapshotId, DateTimeOffset LastWriteTime, string Description, ImmutableArray<byte> ImageData);