namespace PlumbBuddy.Services;

public sealed class UserInterfaceMessaging :
    IUserInterfaceMessaging
{
    public event EventHandler<BeginManifestingModRequestedEventArgs>? BeginManifestingModRequested;
    public event EventHandler<IsModScaffoldedInquiredEventArgs>? IsModScaffoldedInquired;

    public void BeginManifestingMod(string modFilePath) =>
        StaticDispatcher.Dispatch(() => BeginManifestingModRequested?.Invoke(this, new BeginManifestingModRequestedEventArgs
        {
            ModFilePath = modFilePath
        }));

    public async Task<bool> IsModScaffoldedAsync(string modFilePath)
    {
        var eventArgs = new IsModScaffoldedInquiredEventArgs
        {
            ModFilePath = modFilePath
        };
        foreach (var @delegate in IsModScaffoldedInquired?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
        {
            if (@delegate is EventHandler<IsModScaffoldedInquiredEventArgs> eventHandler)
            {
                var tcs = new TaskCompletionSource();
                StaticDispatcher.Dispatch(() =>
                {
                    eventHandler(this, eventArgs);
                    tcs.SetResult();
                });
                await tcs.Task.ConfigureAwait(false);
                if (eventArgs.IsModScaffolded is { } result)
                    return result;
            }
        }
        return false;
    }
}
