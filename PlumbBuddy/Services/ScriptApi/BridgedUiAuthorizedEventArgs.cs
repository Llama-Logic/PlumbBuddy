namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiAuthorizedEventArgs :
    BridgedUiEventArgs
{
    public ICSharpCode.SharpZipLib.Zip.ZipFile? Archive { get; init; }
    public string? TabIconPath { get; init; }
    public required string TabName { get; init; }
    public required string UiRoot { get; init; }
}
