namespace PlumbBuddy.Services;

public interface IMainThreadDetails
{
    SynchronizationContext SynchronizationContext { get; }
}
