namespace PlumbBuddy.Services;

static class Configuration
{
    public static IServiceCollection AddPlumbBuddyServices(this IServiceCollection services)
    {
        services.AddSingleton<IBlazorFramework, BlazorFramework>();
        services.AddSingleton<ICustomThemes, CustomThemes>();
        services.AddSingleton<IPlayer, Player>();
        services.AddSingleton<IPublicCatalogs, PublicCatalogs>();
        services.AddSingleton<IModsDirectoryCataloger, ModsDirectoryCataloger>();
        services.AddSingleton<ISmartSimObserver, SmartSimObserver>();
        services.AddSingleton<ISuperSnacks, SuperSnacks>();
        services.AddSingleton<IGlobalExceptionCatcher, GlobalExceptionCatcher>();
        services.AddSingleton<IUpdateManager, UpdateManager>();
        return services;
    }
}
