namespace PlumbBuddy.Services.Archival;

public delegate Task<string> AsyncScrapbookImageRevisionMetadataRetrievalDelegate(long? snapshotId, ResourceKey key);