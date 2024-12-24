namespace PlumbBuddy;

public partial class App :
    Application
{
    //const string packCodesModelMigration = "20241015012905_ModelV1";

    public App(ILifetimeScope lifetimeScope, ILogger<App> logger, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IAppLifecycleManager appLifecycleManager, IUserInterfaceMessaging userInterfaceMessaging)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        this.settings = settings;
        this.appLifecycleManager = appLifecycleManager;
        this.userInterfaceMessaging = userInterfaceMessaging;
        using var pbDbContext = pbDbContextFactory.CreateDbContext();
        var allMigrations = pbDbContext.Database.GetMigrations();
        //if (!allMigrations.Contains(packCodesModelMigration))
        //    throw new InvalidOperationException("The pack codes model migration was removed. This logic needs to be refactored.");
        var pendingMigrations = pbDbContext.Database.GetPendingMigrations();
        try
        {
            pbDbContext.Database.Migrate();
        }
        catch (SqliteException)
        {
            logger.LogInformation("The preceding error occurred because the migration succession has been broken by my developers. I will recover now by rebuilding your database. I'm sorry that I'll have to scan all your mods again.");
            var objects = new List<(string? name, string? type)>();
            var sqliteConnection = pbDbContext.Database.GetDbConnection();
            var wasClosed = sqliteConnection.State is ConnectionState.Closed;
            if (wasClosed)
                sqliteConnection.Open();
            using (var queryObjectsCmd = sqliteConnection.CreateCommand())
            {
                queryObjectsCmd.CommandText = "SELECT name, type FROM sqlite_master";
                queryObjectsCmd.CommandType = CommandType.Text;
                using var queryObjectsReader = queryObjectsCmd.ExecuteReader();
                while (queryObjectsReader.Read())
                    objects.Add((queryObjectsReader.GetString(0), queryObjectsReader.GetString(1)));
            }
            void executeDbCommand(string commandText)
            {
                using var sqlliteCommand = sqliteConnection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                sqlliteCommand.CommandText = commandText;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                sqlliteCommand.CommandType = CommandType.Text;
                sqlliteCommand.ExecuteNonQuery();
            }
            executeDbCommand("PRAGMA foreign_keys = OFF");
            var objectsByType = objects.ToLookup(@object => @object.type, @object => @object.name);
            foreach (var trigger in objectsByType["trigger"])
                executeDbCommand($"DROP TRIGGER IF EXISTS {trigger}");
            foreach (var index in objectsByType["index"])
            {
                try
                {
                    executeDbCommand($"DROP INDEX IF EXISTS {index}");
                }
                catch (SqliteException)
                {
                    // PK or UNIQUE, that's fine
                    continue;
                }
            }
            foreach (var view in objectsByType["view"])
                executeDbCommand($"DROP VIEW IF EXISTS {view}");
            foreach (var table in objectsByType["table"])
            {
                try
                {
                    executeDbCommand($"DROP TABLE IF EXISTS {table}");
                }
                catch (SqliteException)
                {
                    // system table, that's fine
                    continue;
                }
            }
            executeDbCommand("PRAGMA foreign_keys = ON");
            executeDbCommand("VACUUM");
            if (wasClosed)
                sqliteConnection.Close();
            pendingMigrations = pbDbContext.Database.GetPendingMigrations();
            pbDbContext.Database.Migrate();
        }
        //if (pendingMigrations.Contains(packCodesModelMigration))
        //{
        //    pbDbContext.PackCodes.Add(new PackCode { Code = "FP01" });
        //    for (var ep = 1; ep <= 20; ++ep)
        //        pbDbContext.PackCodes.Add(new PackCode { Code = $"EP{ep:00}" });
        //    for (var gp = 1; gp <= 20; ++gp)
        //        pbDbContext.PackCodes.Add(new PackCode { Code = $"GP{gp:00}" });
        //    for (var sp = 1; sp <= 60; ++sp)
        //        pbDbContext.PackCodes.Add(new PackCode { Code = $"SP{sp:00}" });
        //    pbDbContext.SaveChanges();
        //}
#if DEBUG
        var naughtyStrings = new List<string>();
        var numbers = Enumerable.Range(0, 20).Select(i => (object?)i).ToArray();
        foreach (var property in typeof(AppText)
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string)))
        {
            try
            {
#pragma warning disable CA1806 // Do not ignore method results
                string.Format((string)property.GetValue(null)!, numbers);
#pragma warning restore CA1806 // Do not ignore method results
            }
            catch
            {
                naughtyStrings.Add(property.Name);
            }
        }
        if (naughtyStrings.Count is > 0)
        {
            logger.LogError($"the following localization resources have invalid formats: {naughtyStrings.Humanize()}");
            if (Debugger.IsAttached)
                Debugger.Break();
        }
#endif
        // I didn't inject you directly so that I could make sure I migrated before I woke you up
        // but guess what, it's time for school
        lifetimeScope.Resolve<ISmartSimObserver>();
        InitializeComponent();
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ISettings settings;
    readonly IUserInterfaceMessaging userInterfaceMessaging;

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage(settings, appLifecycleManager, userInterfaceMessaging));
}
