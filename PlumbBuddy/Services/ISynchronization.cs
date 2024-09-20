namespace PlumbBuddy.Services;

public interface ISynchronization
{
    AsyncLock EntityFrameworkCoreDatabaseContextWriteLock { get; } 
}
