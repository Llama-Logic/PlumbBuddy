namespace PlumbBuddy.App.Services;

static class Configuration
{
    public static IServiceCollection AddPlumbBuddyServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlayer, Player>();
        services.AddSingleton<IModsDirectoryCataloger, ModsDirectoryCataloger>();
        services.AddSingleton<ISmartSimObserver, SmartSimObserver>();
        services.AddSingleton<ISuperSnacks, SuperSnacks>();
        return services;
    }
}
