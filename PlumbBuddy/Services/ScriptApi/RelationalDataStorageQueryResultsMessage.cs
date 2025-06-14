namespace PlumbBuddy.Services.ScriptApi;

public class RelationalDataStorageQueryResultsMessage
{
    public int ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public double ExecutionSeconds { get; set; }
    public bool IsSaveSpecific { get; set; }
    public Guid QueryId { get; set; }
    public IList<RelationalDataStorageQueryRecordSet> RecordSets { get; } = [];
    public string? Tag { get; set; }
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}