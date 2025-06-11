namespace PlumbBuddy.Services.ScriptApi;

public class RelationalDataStorageQueryRecordSet
{
    public IList<string> FieldNames { get; } = [];
    public IList<IList<object?>> Records { get; } = [];
}