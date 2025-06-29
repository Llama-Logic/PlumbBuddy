namespace PlumbBuddy.Services;

class GlobalExceptionCatcher :
    IGlobalExceptionCatcher
{
    public GlobalExceptionCatcher(ILogger<IGlobalExceptionCatcher> logger, IAppLifecycleManager appLifecycleManager, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.logger = logger;
        this.appLifecycleManager = appLifecycleManager;
        this.superSnacks = superSnacks;
        AppDomain.CurrentDomain.UnhandledException += HandleCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleTaskSchedulerUnobservedTaskException;
        appLifecycleManager.UnhandledException += HandleAppLifecycleManagerUnhandledException;
    }

    ~GlobalExceptionCatcher() =>
        Dispose(false);

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ILogger<IGlobalExceptionCatcher> logger;
    readonly ISuperSnacks superSnacks;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppDomain.CurrentDomain.UnhandledException -= HandleCurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException -= HandleTaskSchedulerUnobservedTaskException;
        }
    }

    void HandleAppLifecycleManagerUnhandledException(object? sender, AppLifecycleUnhandledExceptionEventArgs e) =>
        ProcessException(e.Exception, false);

    void HandleCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ProcessException(ex, e.IsTerminating);
    }

    void HandleTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        ProcessException(e.Exception, false);
    }

    void ProcessException(Exception ex, bool dying)
    {
        if (dying)
        {
            logger.LogCritical(ex, "Unhandled exception will cause the process to terminate.");
            return;
        }
        logger.LogError(ex, "Unhandled exception observed.");
#if DEBUG
        var exceptionStrs = new List<string>();
        void stringify(Exception ex)
        {
            exceptionStrs.Add($"{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            if (ex is AggregateException aggregateEx)
            {
                foreach (var aggregateInnerEx in aggregateEx.InnerExceptions)
                    stringify(aggregateInnerEx);
                return;
            }
            if (ex.InnerException is { } innerEx)
            {
                stringify(innerEx);
                return;
            }
        }
        stringify(ex.Demystify());
        superSnacks.OfferRefreshments
        (
            new MarkupString(string.Join($"{Environment.NewLine}{Environment.NewLine}", exceptionStrs)),
            Severity.Error,
            options =>
            {
                options.Icon = MaterialDesignIcons.Normal.CodeNotEqual;
                options.RequireInteraction = true;
            }
        );
#else
        superSnacks.OfferRefreshments
        (
            new MarkupString("Something unexpected just happened in the back room. I don't want to alarm you or anything, but it might be a good idea to quit and restart PlumbBuddy soon. I just wrote some important information to my log file which you might want to share with my creators."),
            Severity.Error,
            options =>
            {
                options.Icon = MaterialDesignIcons.Normal.CodeNotEqual;
                options.RequireInteraction = true;
            });
#endif
    }
}
