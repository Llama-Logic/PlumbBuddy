namespace PlumbBuddy;

class AsyncDebouncer
{
    public AsyncDebouncer(Func<Task> asyncAction, TimeSpan debouncingInterval)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);
        this.asyncAction = asyncAction;
        this.debouncingInterval = debouncingInterval;
        schedulingLock = new();
    }

    readonly Func<Task> asyncAction;
    readonly TimeSpan debouncingInterval;
    CancellationTokenSource? performanceCancellationTokenSource;
    readonly AsyncLock schedulingLock;

    public void Execute()
    {
        try
        {
            using var schedulingLockHeld = schedulingLock.Lock(new CancellationToken(true));
            if (schedulingLockHeld is null)
                return;
            performanceCancellationTokenSource?.Cancel();
            performanceCancellationTokenSource?.Dispose();
            performanceCancellationTokenSource = new();
            _ = Task.Run(async () => await PerformActionAsync(performanceCancellationTokenSource.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    async Task PerformActionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(debouncingInterval, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        using var schedulingLockHeld = await schedulingLock.LockAsync(cancellationToken).ConfigureAwait(false);
        await asyncAction().ConfigureAwait(false);
        performanceCancellationTokenSource?.Dispose();
        performanceCancellationTokenSource = null;
    }
}
