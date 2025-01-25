namespace PlumbBuddy.Services;

static class Configuration
{
    public static IServiceCollection AddPlumbBuddyServices(this IServiceCollection services)
    {
        services.AddSingleton<IBlazorFramework, BlazorFramework>();
        services.AddSingleton<ICustomThemes, CustomThemes>();
        services.AddSingleton<ISettings, Settings>();
        services.AddSingleton<IPublicCatalogs, PublicCatalogs>();
        services.AddSingleton<IMarkupLocalizer, MarkupLocalizer>();
        services.AddSingleton(typeof(IMarkupLocalizer<>), typeof(MarkupLocalizer<>));
        services.AddSingleton<IModsDirectoryCataloger, ModsDirectoryCataloger>();
        services.AddSingleton<ISmartSimObserver, SmartSimObserver>();
        services.AddSingleton<ISuperSnacks, SuperSnacks>();
        services.AddSingleton<IGlobalExceptionCatcher, GlobalExceptionCatcher>();
        services.AddSingleton<IUpdateManager, UpdateManager>();
        services.AddSingleton<IUserInterfaceMessaging, UserInterfaceMessaging>();
        services.AddSingleton<ICatalog, Catalog>();
        services.AddSingleton<IArchivist, Archivist>();
        return services;
    }
}
