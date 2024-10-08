namespace PlumbBuddy.Services;

static class Configuration
{
    public static IServiceCollection AddPlumbBuddyServices(this IServiceCollection services)
    {
        services.AddSingleton<ISynchronization, Synchronization>();
        services.AddSingleton<IPlayer, Player>();
        services.AddSingleton<IModsDirectoryCataloger, ModsDirectoryCataloger>();
        services.AddSingleton<ISmartSimObserver, SmartSimObserver>();
        services.AddSingleton<ISuperSnacks, SuperSnacks>();
        services.AddSingleton<IGlobalExceptionCatcher, GlobalExceptionCatcher>();
        return services;
    }
}
