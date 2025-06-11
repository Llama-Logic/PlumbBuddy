namespace PlumbBuddy.Services.ScriptApi;

public class QueryRelationalDataStorageMessage
{
    public bool IsSaveSpecific { get; set; }
    public IList<object?> Parameters { get; } = [];
    public required string Query { get; set; }
    public Guid QueryId { get; set; }
    public string? Tag { get; set; }
    public Guid UniqueId { get; set; }
}
