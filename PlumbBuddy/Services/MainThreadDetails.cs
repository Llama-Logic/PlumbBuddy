namespace PlumbBuddy.Services;

public sealed class MainThreadDetails(SynchronizationContext synchronizationContext) :
    IMainThreadDetails
{
    public SynchronizationContext SynchronizationContext { get; } = synchronizationContext ?? throw new InvalidOperationException("no sync context available on thread");
}
