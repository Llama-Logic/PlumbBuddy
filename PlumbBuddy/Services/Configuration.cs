namespace PlumbBuddy.Services;

static class Configuration
{
    public static IServiceCollection AddPlumbBuddyServices(this IServiceCollection services)
    {
        if (SynchronizationContext.Current is not { } syncContext)
            throw new InvalidOperationException("no sync context available on thread");
        services.AddSingleton<IMainThreadDetails, MainThreadDetails>(_ => new(syncContext));
        services.AddSingleton<IBlazorFramework, BlazorFramework>();
        services.AddSingleton<ICustomThemes, CustomThemes>();
        services.AddSingleton<ISettings, Settings>();
        services.AddSingleton<IPublicCatalogs, PublicCatalogs>();
        services.AddSingleton<IMarkupLocalizer, MarkupLocalizer>();
        services.AddSingleton(typeof(IMarkupLocalizer<>), typeof(MarkupLocalizer<>));
        services.AddSingleton<IGameResourceCataloger, GameResourceCataloger>();
        services.AddSingleton<IModsDirectoryCataloger, ModsDirectoryCataloger>();
        services.AddSingleton<ISmartSimObserver, SmartSimObserver>();
        services.AddSingleton<ISuperSnacks, SuperSnacks>();
        services.AddSingleton<IGlobalExceptionCatcher, GlobalExceptionCatcher>();
        services.AddSingleton<IUpdateManager, UpdateManager>();
        services.AddSingleton<IUserInterfaceMessaging, UserInterfaceMessaging>();
        services.AddSingleton<ICatalog, Catalog>();
        services.AddSingleton<IArchivist, Archivist>();
        services.AddSingleton<IParlay, Parlay>();
        services.AddSingleton<IModHoundClient, ModHoundClient>();
        services.AddSingleton<IProxyHost, ProxyHost>();
        services.AddSingleton<IPersonalNotes, PersonalNotes>();
        return services;
    }
}
