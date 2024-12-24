namespace PlumbBuddy.Services;

public class Archivist :
    IArchivist
{
    static readonly TimeSpan oneSecond = TimeSpan.FromSeconds(1);

    public Archivist(ILogger<Archivist> logger, ISettings settings, ISuperSnacks superSnacks)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(superSnacks);
        this.logger = logger;
        this.settings = settings;
        this.superSnacks = superSnacks;
        connectionLock = new();
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        ConnectToFolders();
    }

    ~Archivist() =>
        Dispose(false);

    IDbContextFactory<ArchiveDbContext>? archiveDbContextFactory;
    readonly AsyncLock connectionLock;
    FileSystemWatcher? fileSystemWatcher;
    bool isDisposed;
    readonly ILogger<Archivist> logger;
    AsyncProducerConsumerQueue<string>? pathsProcessingQueue;
    readonly ISettings settings;
    readonly ISuperSnacks superSnacks;

    public event PropertyChangedEventHandler? PropertyChanged;

    void ConnectToFolders() =>
        Task.Run(ConnectToFoldersAsync);

    async Task ConnectToFoldersAsync()
    {
        using var connectionLockHeld = await connectionLock.LockAsync().ConfigureAwait(false);
        if (fileSystemWatcher is not null)
            return;
        var savesFolder = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"));
        if (!savesFolder.Exists)
            return;
        var archiveFolder = new DirectoryInfo(settings.ArchiveFolderPath);
        if (!archiveFolder.Exists)
        {
            var creationStack = new Stack<DirectoryInfo>();
            creationStack.Push(archiveFolder);
            var archiveAncestorFolder = archiveFolder.Parent;
            while (archiveAncestorFolder is not null && !archiveAncestorFolder.Exists)
            {
                creationStack.Push(archiveAncestorFolder);
                archiveAncestorFolder = archiveAncestorFolder.Parent;
            }
            while (creationStack.TryPop(out var folderToCreate))
                folderToCreate.Create();
        }
        archiveDbContextFactory = new ArchiveDbContextFactory(archiveFolder);
        using var archiveDbContext = await archiveDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        try
        {
            await archiveDbContext.Database.MigrateAsync().ConfigureAwait(false);
        }
        catch (SqliteException)
        {
            logger.LogInformation("The preceding error occurred because the migration succession has been broken by my developers. I will recover now by rebuilding your database. I'm sorry that I'll have to scan all your mods again.");
            var objects = new List<(string? name, string? type)>();
            var sqliteConnection = archiveDbContext.Database.GetDbConnection();
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
            Task executeDbCommandAsync(string commandText)
            {
                using var sqlliteCommand = sqliteConnection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                sqlliteCommand.CommandText = commandText;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                sqlliteCommand.CommandType = CommandType.Text;
                return sqlliteCommand.ExecuteNonQueryAsync();
            }
            await executeDbCommandAsync("PRAGMA foreign_keys = OFF").ConfigureAwait(false);
            var objectsByType = objects.ToLookup(@object => @object.type, @object => @object.name);
            foreach (var trigger in objectsByType["trigger"])
                await executeDbCommandAsync($"DROP TRIGGER IF EXISTS {trigger}").ConfigureAwait(false);
            foreach (var index in objectsByType["index"])
            {
                try
                {
                    await executeDbCommandAsync($"DROP INDEX IF EXISTS {index}").ConfigureAwait(false);
                }
                catch (SqliteException)
                {
                    // PK or UNIQUE, that's fine
                    continue;
                }
            }
            foreach (var view in objectsByType["view"])
                await executeDbCommandAsync($"DROP VIEW IF EXISTS {view}").ConfigureAwait(false);
            foreach (var table in objectsByType["table"])
            {
                try
                {
                    await executeDbCommandAsync($"DROP TABLE IF EXISTS {table}").ConfigureAwait(false);
                }
                catch (SqliteException)
                {
                    // system table, that's fine
                    continue;
                }
            }
            await executeDbCommandAsync("PRAGMA foreign_keys = ON").ConfigureAwait(false);
            await executeDbCommandAsync("VACUUM").ConfigureAwait(false);
            if (wasClosed)
                sqliteConnection.Close();
            await archiveDbContext.Database.MigrateAsync().ConfigureAwait(false);
        }
        _ = Task.Run(ProcessPathsQueueAsync);
    }

    void DisconnectFromFolders() =>
        Task.Run(DisconnectFromFoldersAsync);

    async Task DisconnectFromFoldersAsync()
    {
        using var connectionLockHeld = await connectionLock.LockAsync().ConfigureAwait(false);
        if (fileSystemWatcher is not null)
        {
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }
        pathsProcessingQueue = null;
        archiveDbContextFactory = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing && !isDisposed)
        {
            DisconnectFromFolders();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
            isDisposed = true;
        }
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ArchiveFolderPath)
            or nameof(ISettings.UserDataFolderPath))
            ReconnectFolders();
        if (e.PropertyName is nameof(ISettings.ArchivingEnabled))
        {
            if (settings.ArchivingEnabled)
                ConnectToFolders();
            else
                DisconnectFromFolders();
        }
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    async Task ProcessDequeuedFileAsync(DirectoryInfo savesDirectoryInfo, FileInfo fileInfo)
    {
        var path = fileInfo.FullName[(savesDirectoryInfo.FullName.Length + 1)..];
        try
        {
        }
        catch (DirectoryNotFoundException)
        {
            // if this happens we really don't care because whatever enqueued paths are next will clear it up
            return;
        }
        catch (FileNotFoundException)
        {
            // if this happens we really don't care because whatever enqueued paths are next will clear it up
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unexpected exception encountered while processing {FilePath}", path);
            superSnacks.OfferRefreshments(new MarkupString(string.Format(
                """
                <h3>Whoops!</h3>
                I ran into a problem trying to archive the mod file at this location:<br />
                <strong>{0}</strong><br />
                <br />
                Brief technical details:<br />
                <span style="font-family: monospace;">{1}: {2}</span><br />
                <br />
                More detailed technical information is available in my log.
                """, path, ex.GetType().Name, ex.Message)), Severity.Warning, options =>
            {
                options.RequireInteraction = true;
                options.Icon = MaterialDesignIcons.Normal.ContentSaveAlert;
            });
        }
    }

    async Task ProcessPathsQueueAsync()
    {
        while (pathsProcessingQueue is not null
            && await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var nomNom = new Queue<string>();
            nomNom.Enqueue(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
            while (true)
            {
                await Task.Delay(oneSecond).ConfigureAwait(false);
                try
                {
                    if (!await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                        break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                try
                {
                    while (await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false))
                    {
                        var path = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                        if (!nomNom.Contains(path))
                            nomNom.Enqueue(path);
                    }
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            try
            {
                while (nomNom.TryDequeue(out var path))
                {
                    var savesDirectoryInfo = new DirectoryInfo(fileSystemWatcher!.Path);
                    var fullPath = Path.Combine(savesDirectoryInfo.FullName, path);
                    if (File.Exists(fullPath))
                        await ProcessDequeuedFileAsync(savesDirectoryInfo, new FileInfo(fullPath)).ConfigureAwait(false);
                    else if (Directory.Exists(fullPath))
                    {
                        var savesDirectoryFile = new DirectoryInfo(fullPath).GetFiles("*.*", SearchOption.TopDirectoryOnly).ToImmutableArray();
                        using var semaphore = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));
                        await Task.WhenAll(savesDirectoryFile.Select(async fileInfo =>
                        {
                            await semaphore.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                await ProcessDequeuedFileAsync(savesDirectoryInfo, fileInfo).ConfigureAwait(false);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        })).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unhandled exception while processing the paths queue");
            }
            finally
            {
            }
        }
    }

    void ReconnectFolders() =>
        Task.Run(ReconnectFoldersAsync);

    async Task ReconnectFoldersAsync()
    {
        await DisconnectFromFoldersAsync().ConfigureAwait(false);
        await ConnectToFoldersAsync().ConfigureAwait(false);
    }
}
