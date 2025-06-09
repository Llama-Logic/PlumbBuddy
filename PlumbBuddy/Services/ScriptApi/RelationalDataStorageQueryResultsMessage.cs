namespace PlumbBuddy.Services.ScriptApi;

public class RelationalDataStorageQueryResultsMessage
{
    public int ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSaveSpecific { get; set; }
    public Guid QueryId { get; set; }
    public IList<IList<IDictionary<string, object?>>> RecordSets { get; } = [];
    public required string Type { get; set; }
    public Guid UniqueId { get; set; }
}