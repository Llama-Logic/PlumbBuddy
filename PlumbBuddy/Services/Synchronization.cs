namespace PlumbBuddy.Services;

class Synchronization :
    ISynchronization
{
    public AsyncLock EntityFrameworkCoreDatabaseContextWriteLock { get; } = new();
}
