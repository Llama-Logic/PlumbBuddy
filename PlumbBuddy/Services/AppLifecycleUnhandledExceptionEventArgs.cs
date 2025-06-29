namespace PlumbBuddy.Services;

public class AppLifecycleUnhandledExceptionEventArgs :
    EventArgs
{
    public required Exception Exception { get; init; }
}
