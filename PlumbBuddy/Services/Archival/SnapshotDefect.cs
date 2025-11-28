namespace PlumbBuddy.Services.Archival;

public class SnapshotDefect
{
    public required string Description { get; init; }
    public SavePackageSnapshotDefectType Type { get; init; }
}
