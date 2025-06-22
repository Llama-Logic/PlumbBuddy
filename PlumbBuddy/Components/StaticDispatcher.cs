namespace PlumbBuddy.Components;

static class StaticDispatcher
{
    static IDispatcher? dispatcher;
    static readonly AsyncManualResetEvent dispatcherSetManualResetEvent = new(false);

    public static Task DispatcherSet =>
        WaitForDispatcherSetAsync();

    public static bool IsDispatcherSet =>
        dispatcher is not null;

    public static void RegisterDispatcher(IDispatcher dispatcher)
    {
        if (StaticDispatcher.dispatcher is not null)
            throw new InvalidOperationException($"{nameof(RegisterDispatcher)} already called");
        ArgumentNullException.ThrowIfNull(dispatcher);
        StaticDispatcher.dispatcher = dispatcher;
        dispatcherSetManualResetEvent.Set();
    }

    public static void Dispatch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (dispatcher is null)
            throw new InvalidOperationException($"{nameof(RegisterDispatcher)} hasn't been called");
        if (dispatcher.IsDispatchRequired)
            dispatcher.Dispatch(action);
        else
            action();
    }

    public static async Task DispatchAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (dispatcher is null)
            throw new InvalidOperationException($"{nameof(RegisterDispatcher)} hasn't been called");
        if (dispatcher.IsDispatchRequired)
            await dispatcher.DispatchAsync(action);
        else
            action();
    }

    public static async Task DispatchAsync(Func<Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);
        if (dispatcher is null)
            throw new InvalidOperationException($"{nameof(RegisterDispatcher)} hasn't been called");
        if (dispatcher.IsDispatchRequired)
            await dispatcher.DispatchAsync(asyncAction);
        else
            await asyncAction();
    }

    public static async Task<T> DispatchAsync<T>(Func<Task<T>> asyncFunc)
    {
        ArgumentNullException.ThrowIfNull(asyncFunc);
        if (dispatcher is null)
            throw new InvalidOperationException($"{nameof(RegisterDispatcher)} hasn't been called");
        return dispatcher.IsDispatchRequired
            ? await dispatcher.DispatchAsync(asyncFunc)
            : await asyncFunc();
    }

    public static Task WaitForDispatcherSetAsync(CancellationToken cancellationToken = default) =>
        dispatcherSetManualResetEvent.WaitAsync(cancellationToken);
}
