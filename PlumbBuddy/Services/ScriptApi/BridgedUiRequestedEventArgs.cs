namespace PlumbBuddy.Services.ScriptApi;

public class BridgedUiRequestedEventArgs(TaskCompletionSource<bool> playerResponseTaskCompletionSource) :
    EventArgs
{
    public required string RequestorName { get; init; }
    public required string RequestReason { get; init; }
    public required string TabName { get; init; }

    public void Authorize() =>
        playerResponseTaskCompletionSource.SetResult(true);

    public void Deny() =>
        playerResponseTaskCompletionSource.SetResult(false);
}
