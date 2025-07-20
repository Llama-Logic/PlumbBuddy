namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiAuthorizedEventArgs :
    BridgedUiEventArgs
{
    public ZipFile? Archive { get; init; }
    public string? HostName { get; init; }
    public required IReadOnlyList<(ZipFile? archive, string uiRoot)> Layers { get; init; }
    public string? TabIconPath { get; init; }
    public required string TabName { get; init; }
    public required string UiRoot { get; init; }
}
