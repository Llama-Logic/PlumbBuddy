using AngleSharp.Io;
using SQLitePCL;
using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services;

[SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "This is for Python, CA. You have your head up your ass.")]
[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class ProxyHost :
    IProxyHost
{
    static readonly JsonSerializerOptions bridgedUiJsonSerializerOptions = AddConverters(new()
    {
        AllowOutOfOrderMetadataProperties = true,
        AllowTrailingCommas = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = false
    });
    static readonly TimeSpan oneQuarterSecond = TimeSpan.FromSeconds(0.25);
    const int port = 7342;
    static readonly JsonSerializerOptions proxyJsonSerializerOptions = AddConverters(new()
    {
        AllowOutOfOrderMetadataProperties = true,
        AllowTrailingCommas = true,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = false
    });

    static JsonSerializerOptions AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new PermissiveGuidConverter());
        return options;
    }

    static async Task<DirectoryInfo> GetAppDataDirectoryAsync()
    {
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        await StaticDispatcher.DispatcherSet.ConfigureAwait(false);
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.AppDataDirectory));
        return await tcs.Task.ConfigureAwait(false);
    }

    static async Task<DirectoryInfo> GetCacheDirectoryAsync()
    {
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        await StaticDispatcher.DispatcherSet.ConfigureAwait(false);
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.CacheDirectory));
        return await tcs.Task.ConfigureAwait(false);
    }

    [GeneratedRegex(@"^\.ver\d$", RegexOptions.IgnoreCase)]
    private static partial Regex GetBackupSaveFileExtensionPattern();

    [GeneratedRegex(@"[\da-f]{64}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetSha256HashHexPattern();

    [GeneratedRegex(@"^[a-z\d\-]+$")]
    private static partial Regex GetValidDnsHostnamePattern();

    static bool TryParseMessage<T>(JsonElement messageRoot, string messageJson, JsonSerializerOptions jsonSerializerOptions, ILogger<ProxyHost> logger, string messageDescription, [NotNullWhen(true)] out T? message)
        where T : class
    {
        try
        {
            if (messageRoot.Deserialize<T>(jsonSerializerOptions) is not { } nullableMessage)
            {
                logger.LogWarning("failed to parse {MessageDescription} (value was null): {Message}", messageDescription, messageJson);
                message = null;
                return false;
            }
            message = nullableMessage;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to parse {MessageDescription}: {Message}", messageDescription, messageJson);
            message = null;
            return false;
        }
    }

    public ProxyHost(IPlatformFunctions platformFunctions, ILogger<ProxyHost> logger, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IAppLifecycleManager appLifecycleManager)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        this.platformFunctions = platformFunctions;
        this.logger = logger;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.appLifecycleManager = appLifecycleManager;
        cancellationTokenSource = new();
        cancellationToken = cancellationTokenSource.Token;
        connectedClients = new();
        bridgedUiLoadingLocks = new();
        loadedBridgedUis = new();
        gameIsSavingManualResetEvent = new(true);
        reserveSavesAccessManualResetEvent = new(true);
        gameServicesRunningManualResetEvent = new(false);
        saveSpecificDataStorageConnectionLocks = new();
        saveSpecificDataStorageConnections = new();
        saveSpecificDataStorageInitializationLock = new();
        saveSpecificDataStorageProcessingIdentifiersDenyList = new();
        saveSpecificDataStoragePropagationLock = new();
        pendingSaveSpecificDataStorageInitialization = true;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        listener = new(IPAddress.Loopback, port);
        listener.Start();
        _ = Task.Run(ListenAsync);
    }

    ~ProxyHost() =>
        Dispose(false);

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ConcurrentDictionary<Guid, AsyncLock> bridgedUiLoadingLocks;
    readonly CancellationTokenSource cancellationTokenSource;
    readonly CancellationToken cancellationToken;
    readonly ConcurrentDictionary<TcpClient, AsyncLock> connectedClients;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? fileSystemWatcher;
    bool isBridgedUiDevelopmentModeEnabled;
    bool isClientConnected;
    readonly TcpListener listener;
    readonly ConcurrentDictionary<Guid, ZipFile?> loadedBridgedUis;
    readonly ILogger<ProxyHost> logger;
    AsyncProducerConsumerQueue<string>? pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly AsyncManualResetEvent gameIsSavingManualResetEvent;
    readonly AsyncManualResetEvent gameServicesRunningManualResetEvent;
    bool pendingSaveSpecificDataStorageInitialization;
    readonly IPlatformFunctions platformFunctions;
    readonly AsyncManualResetEvent reserveSavesAccessManualResetEvent;
    readonly ConcurrentDictionary<Guid, AsyncLock> saveSpecificDataStorageConnectionLocks;
    readonly ConcurrentDictionary<Guid, SqliteConnection> saveSpecificDataStorageConnections;
    ulong saveCreated;
    ulong saveNucleusId;
    ulong saveSimNow;
    ulong saveSlotId;
    readonly AsyncLock saveSpecificDataStorageInitializationLock;
    readonly ConcurrentDictionary<(ulong nucleusId, ulong created, ulong simNow), bool> saveSpecificDataStorageProcessingIdentifiersDenyList;
    readonly AsyncReaderWriterLock saveSpecificDataStoragePropagationLock;
    Task<(ResourceKey key, ReadOnlyMemory<byte> content)>? saveSpecificDataStorageSnapshot;
    ulong saveSpecificDataStorageSnapshotCreated;
    ulong saveSpecificDataStorageSnapshotNucleusId;
    ulong saveSpecificDataStorageSnapshotSimNow;
    ulong saveSpecificDataStorageSnapshotSlotId;
    readonly ISettings settings;

    public bool IsBridgedUiDevelopmentModeEnabled
    {
        get => isBridgedUiDevelopmentModeEnabled;
        set
        {
            if (isBridgedUiDevelopmentModeEnabled == value)
                return;
            isBridgedUiDevelopmentModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsClientConnected
    {
        get => isClientConnected;
        private set
        {
            if (isClientConnected == value)
                return;
            isClientConnected = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<BridgedUiAuthorizedEventArgs>? BridgedUiAuthorized;
    public event EventHandler<BridgedUiDataSentEventArgs>? BridgedUiDataSent;
    public event EventHandler<BridgedUiEventArgs>? BridgedUiDestroyed;
    public event EventHandler<BridgedUiEventArgs>? BridgedUiDomLoaded;
    public event EventHandler<BridgedUiFocusRequestedEventArgs>? BridgedUiFocusRequested;
    public event EventHandler<BridgedUiRequestedEventArgs>? BridgedUiRequested;
    public event EventHandler<BridgedUiMessageSentEventArgs>? BridgedUiMessageSent;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ProxyConnected;
    public event EventHandler? ProxyDisconnected;
    public event EventHandler<SpecificBridgedUiMessageSentEventArgs>? SpecificBridgedUiMessageSent;

    async Task BridgedUiRequestResolvedAsync(Guid uniqueId, int denialReason)
    {
        await SendMessageToProxyAsync(new BridgedUiRequestResponseMessage
        {
            DenialReason = denialReason,
            Type = nameof(HostMessageType.BridgedUiRequestResponse).Underscore().ToLowerInvariant(),
            UniqueId = uniqueId
        }).ConfigureAwait(false);
        SendMessageToBridgedUis(new BridgedUiRequestResponseMessage
        {
            DenialReason = denialReason,
            Type = nameof(HostMessageType.BridgedUiRequestResponse).Camelize(),
            UniqueId = uniqueId
        });
    }

    async Task<ZipFile?> CacheScriptModAsync(string modsDirectoryRelativePath, Guid uniqueId)
    {
        var path = Path.Combine(settings.UserDataFolderPath, "Mods", modsDirectoryRelativePath);
        if (!File.Exists(path))
            return null;
        var cacheDirectory = await GetCacheDirectoryAsync().ConfigureAwait(false);
        var cachePath = Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs");
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);
        cachePath = Path.Combine(cachePath, $"{uniqueId:n}.ts4script");
        try
        {
            File.Copy(path, cachePath, true);
        }
        catch (IOException)
        {
            // in use already
        }
        try
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new ZipFile(File.OpenRead(cachePath), false);
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to load script mod at {Path}", path);
            return null;
        }
    }

    void ConnectToSavesFolder()
    {
        var userDataFolder = new DirectoryInfo(settings.UserDataFolderPath);
        if (fileSystemWatcher is not null
            || !userDataFolder.Exists)
            return;
        pathsProcessingQueue = new();
        var savesFolder = Directory.CreateDirectory(Path.Combine(userDataFolder.FullName, "saves"));
        fileSystemWatcher = new FileSystemWatcher(savesFolder.FullName)
        {
            IncludeSubdirectories = false,
            InternalBufferSize = 64 * 1024,
            NotifyFilter =
                  NotifyFilters.CreationTime
                | NotifyFilters.DirectoryName
                | NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
        };
        fileSystemWatcher.Changed += HandleFileSystemWatcherChanged;
        fileSystemWatcher.Created += HandleFileSystemWatcherCreated;
        fileSystemWatcher.Error += HandleFileSystemWatcherError;
        fileSystemWatcher.Renamed += HandleFileSystemWatcherRenamed;
        fileSystemWatcher.EnableRaisingEvents = true;
        _ = Task.Run(ProcessPathsQueueAsync);
    }

    public void DestroyBridgedUi(Guid uniqueId) =>
        _ = Task.Run(async () => await DestroyBridgedUiAsync(uniqueId).ConfigureAwait(false));

    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope")]
    async Task DestroyBridgedUiAsync(Guid uniqueId)
    {
        using var bridgedUiLoadingLockHeld = await bridgedUiLoadingLocks.GetOrAdd(uniqueId, _ => new()).LockAsync().ConfigureAwait(false);
        if (!loadedBridgedUis.TryRemove(uniqueId, out var archive))
            return;
        logger.LogDebug("I now take from bridged UI {UniqueId} its power, in the name of CodeBleu, and Scumbumbo before. I, PlumbBuddy, CAST YOU OFF!", uniqueId);
        BridgedUiDestroyed?.Invoke(this, new() { UniqueId = uniqueId });
        await SendMessageToProxyAsync(new BridgedUiDestroyedMessage
        {
            Type = nameof(HostMessageType.BridgedUiDestroyed).Underscore().ToLowerInvariant(),
            UniqueId = uniqueId
        });
        SendMessageToBridgedUis(new BridgedUiDestroyedMessage
        {
            Type = nameof(HostMessageType.BridgedUiDestroyed).Camelize(),
            UniqueId = uniqueId
        });
        archive?.Close();
        if (archive is IDisposable disposableArchive)
            disposableArchive.Dispose();
        var cacheDirectory = await GetCacheDirectoryAsync().ConfigureAwait(false);
        var cachedArchive = new FileInfo(Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs", $"{uniqueId:n}.ts4script"));
        if (cachedArchive.Exists)
            cachedArchive.Delete();
    }

    void DisconnectFromSavesFolder()
    {
        if (fileSystemWatcher is not null)
        {
            fileSystemWatcher.Changed -= HandleFileSystemWatcherChanged;
            fileSystemWatcher.Created -= HandleFileSystemWatcherCreated;
            fileSystemWatcher.Error -= HandleFileSystemWatcherError;
            fileSystemWatcher.Renamed -= HandleFileSystemWatcherRenamed;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }
        pathsProcessingQueue?.CompleteAdding();
        pathsProcessingQueue = null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            DisconnectFromSavesFolder();
            cancellationTokenSource.Cancel();
            listener.Dispose();
            cancellationTokenSource.Dispose();
        }
    }

    async Task EscortClientAsync(TcpClient client)
    {
        ConnectToSavesFolder();
        var stream = client.GetStream();
        try
        {
            Memory<byte> serializedMessageSizeBuffer = new byte[4];
            while (client.Connected || client.Available is > 0)
            {
                await stream.ReadExactlyAsync(serializedMessageSizeBuffer, cancellationToken).ConfigureAwait(false);
                var serializedMessageSize = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(serializedMessageSizeBuffer.Span));
                var serializedMessageRentedArray = ArrayPool<byte>.Shared.Rent(serializedMessageSize);
                try
                {
                    Memory<byte> serializedMessageBuffer = serializedMessageRentedArray;
                    await stream.ReadExactlyAsync(serializedMessageBuffer[..serializedMessageSize], cancellationToken).ConfigureAwait(false);
                    var messageJson = serializedMessageBuffer[..serializedMessageSize];
                    logger.LogDebug("message received from proxy: {Message}", messageJson);
                    JsonDocument message;
                    try
                    {
                        message = JsonDocument.Parse(messageJson);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "unparsable message from proxy: {Message}", messageJson);
                        continue;
                    }
                    try
                    {
                        await ProcessMessageAsync(message, proxyJsonSerializerOptions).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "unhandled exception while processing message from proxy: {Message}", messageJson);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(serializedMessageRentedArray);
                }
            }
            ProxyDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (EndOfStreamException)
        {
            ProxyDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            ProxyDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ProxyDisconnected?.Invoke(this, EventArgs.Empty);
            logger.LogError(ex, "unexpected unhandled exception while processing proxy communication");
        }
        finally
        {
            if (connectedClients.TryRemove(client, out var clientWriteLock))
            {
                using var clientWriteLockHeld = await clientWriteLock.LockAsync(cancellationToken).ConfigureAwait(false);
                IsClientConnected = !connectedClients.IsEmpty;
                client.Dispose();
            }
            if (connectedClients.IsEmpty)
            {
                saveSpecificDataStorageSnapshot = null;
                DisconnectFromSavesFolder();
                gameIsSavingManualResetEvent.Set();
                reserveSavesAccessManualResetEvent.Set();
                gameServicesRunningManualResetEvent.Reset();
                foreach (var loadedBridgedUiUniqueId in loadedBridgedUis.Keys)
                    await DestroyBridgedUiAsync(loadedBridgedUiUniqueId).ConfigureAwait(false);
            }
        }
    }

    public Task ForegroundPlumbBuddyAsync() =>
        SendMessageToProxyAsync(new ForegroundPlumbBuddyMessage
        {
            Type = nameof(HostMessageType.ForegroundPlumbbuddy).Underscore().ToLowerInvariant()
        });

    void HandleFileSystemWatcherChanged(object sender, FileSystemEventArgs e) =>
        pathsProcessingQueue?.Enqueue(e.FullPath);

    void HandleFileSystemWatcherCreated(object sender, FileSystemEventArgs e) =>
        pathsProcessingQueue?.Enqueue(e.FullPath);

    void HandleFileSystemWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError(e.GetException(), "saves directory monitoring encountered unexpected unhandled exception");
        DisconnectFromSavesFolder();
        ConnectToSavesFolder();
    }

    void HandleFileSystemWatcherRenamed(object sender, RenamedEventArgs e) =>
        pathsProcessingQueue?.Enqueue(e.FullPath);

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Type))
            IsBridgedUiDevelopmentModeEnabled = false;
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task InitializeSaveSpecificRelationalDataStorageAsync()
    {
        using var saveSpecificDataStorageInitializationLockHeld = await saveSpecificDataStorageInitializationLock.LockAsync().ConfigureAwait(false);
        if (!pendingSaveSpecificDataStorageInitialization)
            return;
        pendingSaveSpecificDataStorageInitialization = false;
        await reserveSavesAccessManualResetEvent.WaitAsync().ConfigureAwait(false);
        var content = ReadOnlyMemory<byte>.Empty;
        if (saveSpecificDataStorageSnapshot is { } memoryResidentSnapshot
            && saveSpecificDataStorageSnapshotNucleusId == saveNucleusId
            && saveSpecificDataStorageSnapshotCreated == saveCreated
            && saveSpecificDataStorageSnapshotSlotId == saveSlotId
            && saveSpecificDataStorageSnapshotSimNow <= saveSimNow)
            (_, content) = await memoryResidentSnapshot.ConfigureAwait(false);
        var packagesToDispose = new List<DataBasePackedFile>();
        if (content.IsEmpty)
        {
            try
            {
                var savesDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"));
                if (!savesDirectory.Exists)
                    return;
                var candidates = new List<(ulong simNow, DataBasePackedFile savePackage)>();
                var saveFileNameForSlot = $"Slot_{(uint)saveSlotId:x8}.save";
                static async Task considerCandidateAsync(List<DataBasePackedFile> packagesToDispose, List<(ulong simNow, DataBasePackedFile savePackage)> candidates, ulong saveNucleusId, ulong saveCreated, ulong saveSlotId, ulong saveSimNow, FileInfo saveFile)
                {
                    var savePackage = await DataBasePackedFile.FromPathAsync(saveFile.FullName).ConfigureAwait(false);
                    packagesToDispose.Add(savePackage);
                    var packageKeys = await savePackage.GetKeysAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
                    var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                    if (saveGameDataKeys.Length is <= 0 or >= 2)
                        return;
                    var saveGameDataKey = saveGameDataKeys[0];
                    var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await savePackage.GetAsync(saveGameDataKey).ConfigureAwait(false));
                    if (saveGameData is null
                        || saveGameData.Account is not { } account
                        || account.NucleusId != saveNucleusId
                        || account.Created != saveCreated
                        || saveGameData.SaveSlot is not { } saveSlot
                        || saveSlotId is not 0
                        && saveSlot.SlotId != saveSlotId
                        || saveSlot.GameplayData is not { } gamePlayData
                        || gamePlayData.WorldGameTime > saveSimNow)
                        return;
                    candidates.Add((gamePlayData.WorldGameTime, savePackage));
                }
                foreach (var saveFile in savesDirectory.GetFiles("*.save", SearchOption.TopDirectoryOnly).Where(file => file.Name.Equals(saveFileNameForSlot, StringComparison.OrdinalIgnoreCase)))
                    await considerCandidateAsync(packagesToDispose, candidates, saveNucleusId, saveCreated, saveSlotId, saveSimNow, saveFile).ConfigureAwait(false);
                if (candidates.Count is 0)
                {
                    foreach (var saveFile in savesDirectory.GetFiles("*.save", SearchOption.TopDirectoryOnly).Where(file => !file.Name.Equals(saveFileNameForSlot, StringComparison.OrdinalIgnoreCase)))
                        await considerCandidateAsync(packagesToDispose, candidates, saveNucleusId, saveCreated, saveSlotId, saveSimNow, saveFile).ConfigureAwait(false);
                }
                if (candidates.Count is 0)
                    return;
                var orderedCandidates = candidates
                    .OrderByDescending(t => t.simNow);
                var (simNow, savePackage) = orderedCandidates.First();
                saveSimNow = simNow;
                try
                {
                    var savePackageKeys = await savePackage.GetKeysAsync().ConfigureAwait(false);
                    var saveSpecificDataKey = savePackageKeys.FirstOrDefault(k => k.Type is ResourceType.SaveSpecificRelationalDataStorage);
                    if (saveSpecificDataKey == default)
                        return;
                    content = await savePackage.GetAsync(saveSpecificDataKey).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "uncaught exception");
                }
            }
            finally
            {
                foreach (var packageToDispose in packagesToDispose)
                    await packageToDispose.DisposeAsync().ConfigureAwait(false);
            }
        }
        try
        {
            using var contentStream = new ReadOnlyMemoryOfByteStream(content);
            using var zipInputStream = new ZipInputStream(contentStream);
            ZipEntry entry;
            while ((entry = zipInputStream.GetNextEntry()) is not null)
            {
                if (entry.IsDirectory)
                    continue;
                var entryNameParts = entry.Name.Split('.');
                if (entryNameParts.Length is <= 1)
                    continue;
                if (!Guid.TryParse(string.Join('.', entryNameParts.Take(entryNameParts.Length - 1)), out var uniqueId))
                    continue;
#pragma warning disable CA2000 // Dispose objects before losing scope
                var connection = new SqliteConnection("Data Source=:memory:");
#pragma warning restore CA2000 // Dispose objects before losing scope
                byte[] serializedDatabase;
                using (var serializedDatabaseStream = new MemoryStream())
                {
                    await zipInputStream.CopyToAsync(serializedDatabaseStream).ConfigureAwait(false);
                    serializedDatabase = serializedDatabaseStream.ToArray();
                }
                var serializedDatabaseSize = serializedDatabase.Length;
                var rawSerializedDatabase = raw.sqlite3_malloc64(serializedDatabaseSize);
                Marshal.Copy(serializedDatabase, 0, rawSerializedDatabase, serializedDatabaseSize);
                await connection.OpenAsync().ConfigureAwait(false);
                if (raw.sqlite3_deserialize(connection.Handle, "main", rawSerializedDatabase, serializedDatabaseSize, serializedDatabaseSize, raw.SQLITE_DESERIALIZE_FREEONCLOSE | raw.SQLITE_DESERIALIZE_RESIZEABLE) is not raw.SQLITE_OK)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    continue;
                }
                saveSpecificDataStorageConnections.AddOrUpdate(uniqueId, connection, (_, _) => connection);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "uncaught exception");
        }
    }

    async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                connectedClients.TryAdd(client, new());
                IsClientConnected = !connectedClients.IsEmpty;
                ProxyConnected?.Invoke(this, EventArgs.Empty);
                _ = Task.Run(() => EscortClientAsync(client));
                await SendMessageToProxyAsync(new SendLoadedSaveIdentifiersMessage
                {
                    Type = nameof(HostMessageType.SendLoadedSaveIdentifiers).Underscore().ToLowerInvariant()
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task NotifyScreenshotsChangedAsync()
    {
        await SendMessageToProxyAsync(new HostMessageBase
        {
            Type = nameof(HostMessageType.ScreenshotsChanged).Underscore().ToLowerInvariant()
        }).ConfigureAwait(false);
        SendMessageToBridgedUis(new HostMessageBase
        {
            Type = nameof(HostMessageType.ScreenshotsChanged).Camelize()
        });
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

    async Task ProcessBridgedUiLookUpAsync(BridgedUiLookUpMessage lookUpMessage)
    {
        using var bridgedUiLoadingLockHeld = await bridgedUiLoadingLocks.GetOrAdd(lookUpMessage.UniqueId, _ => new()).LockAsync().ConfigureAwait(false);
        var isLoaded = loadedBridgedUis.ContainsKey(lookUpMessage.UniqueId);
        await SendMessageToProxyAsync(new BridgedUiLookUpResponseMessage
        {
            IsLoaded = isLoaded,
            Type = nameof(HostMessageType.BridgedUiLookUpResponse).Underscore().ToLowerInvariant(),
            UniqueId = lookUpMessage.UniqueId
        }).ConfigureAwait(false);
        SendMessageToBridgedUis(new BridgedUiLookUpResponseMessage
        {
            IsLoaded = isLoaded,
            Type = nameof(HostMessageType.BridgedUiLookUpResponse).Camelize(),
            UniqueId = lookUpMessage.UniqueId
        });
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task ProcessBridgedUiRequestAsync(BridgedUiRequestMessage requestMessage)
    {
        if (requestMessage.HostName is { } hostName
            && !GetValidDnsHostnamePattern().IsMatch(hostName))
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_InvalidHostName).ConfigureAwait(false);
            return;
        }
        using var bridgedUiLoadingLockHeld = await bridgedUiLoadingLocks.GetOrAdd(requestMessage.UniqueId, _ => new()).LockAsync().ConfigureAwait(false);
        if (loadedBridgedUis.ContainsKey(requestMessage.UniqueId))
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_None).ConfigureAwait(false);
            return;
        }
        if (isBridgedUiDevelopmentModeEnabled
            && (Directory.Exists(requestMessage.UiRoot)
            || Uri.TryCreate(requestMessage.UiRoot, UriKind.Absolute, out _)))
        {
            PromptPlayerForBridgedUiAuthorization(requestMessage);
            return;
        }
        var scriptMod = requestMessage.ScriptMod;
        if (scriptMod?.StartsWith(settings.UserDataFolderPath) ?? false)
            scriptMod = scriptMod[settings.UserDataFolderPath.Length..];
        if (scriptMod?.StartsWith(Path.DirectorySeparatorChar) ?? false)
            scriptMod = scriptMod[1..];
        if (scriptMod?.StartsWith($"Mods{Path.DirectorySeparatorChar}") ?? false)
            scriptMod = scriptMod[5..];
        if (scriptMod?.IndexOf(".ts4script", StringComparison.OrdinalIgnoreCase) is { } extensionIndex
            && extensionIndex >= 0)
            scriptMod = scriptMod[..(extensionIndex + 10)];
        if (scriptMod is not null
            && File.Exists(scriptMod))
        {
            // nice try escaping from the Mods folder, but nope
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_ScriptModNotFound).ConfigureAwait(false);
            return;
        }
        if (scriptMod is not null
            && GetSha256HashHexPattern().Match(scriptMod) is { } scriptModManifestHashHexMatch
            && scriptModManifestHashHexMatch.Success)
        {
            var scriptModManifestHash = scriptModManifestHashHexMatch.Value.ToByteSequence().ToArray();
            using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            if (await pbDbContext.ModFiles.Where
                (
                    mf =>
                           mf.FileType == ModsDirectoryFileType.ScriptArchive
                        && mf.ModFileHash.ModFileManifests.Any
                        (
                            mfm =>
                                   mfm.CalculatedModFileManifestHash.Sha256 == scriptModManifestHash
                                || mfm.SubsumedHashes.Any(sh => sh.Sha256 == scriptModManifestHash)
                        )
                )
                .FirstOrDefaultAsync()
                .ConfigureAwait(false) is { } manifestedScriptMod)
                scriptMod = manifestedScriptMod.Path;
        }
        ZipFile? archive = null;
#pragma warning disable CA2000 // Dispose objects before losing scope
        try
        {
            archive = !string.IsNullOrWhiteSpace(scriptMod)
                ? await CacheScriptModAsync(scriptMod, requestMessage.UniqueId).ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to cache script mod while preparing bridged UI {UniqueId} from {Path}", requestMessage.UniqueId, scriptMod);
        }
#pragma warning restore CA2000 // Dispose objects before losing scope
        if (archive is null)
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_ScriptModNotFound).ConfigureAwait(false);
            return;
        }
        if (archive.GetEntry(Path.Combine([..new string[] { requestMessage.UiRoot, "index.html" }.Where(segment => !string.IsNullOrWhiteSpace(segment))]).Replace("\\", "/", StringComparison.Ordinal)) is null)
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_IndexNotFound).ConfigureAwait(false);
            return;
        }
        PromptPlayerForBridgedUiAuthorization(requestMessage, archive);
    }

    async Task ProcessLookUpLocalizedStringsMessageAsync(LookUpLocalizedStringsMessage lookUpLocalizedStringsMessage, Guid? fromBridgedUiUniqueId = null)
    {
        var response = new LookUpLocalizedStringsResponseMessage
        {
            LookUpId = lookUpLocalizedStringsMessage.LookUpId,
            Type = fromBridgedUiUniqueId is null
                ? nameof(HostMessageType.LookUpLocalizedStringsResponse).Underscore().ToLowerInvariant()
                : nameof(HostMessageType.LookUpLocalizedStringsResponse).Camelize()
        };
        if (lookUpLocalizedStringsMessage.LocKeys.Any())
        {
            var queryParts = new List<string>()
            {
                $"""
                WITH AllStringTableEntries AS (
                	SELECT
                		2 Classification,
                		mf.Path PackagePath,
                		mfr.StringTableLocalePrefix Locale,
                		mfr.KeyType,
                		mfr.KeyGroup,
                		mfr.KeyFullInstance,
                		mfste.SignedKey
                	FROM
                		ModFileStringTableEntries mfste
                		JOIN ModFileResources mfr ON mfr.Id = mfste.ModFileResourceId
                		JOIN ModFileHashes mfh ON mfh.Id = mfr.ModFileHashId
                		JOIN ModFiles mf ON mf.ModFileHashId = mfh.Id
                	WHERE
                		mf.Path IS NOT NULL
                		AND mf.FileType = 1
                	UNION
                	SELECT
                		grp.IsDelta Classification,
                		grp.Path PackagePath,
                		grpr.StringTableLocalePrefix Locale,
                		grpr.KeyType,
                		grpr.KeyGroup,
                		grpr.KeyFullInstance,
                		gste.SignedKey
                	FROM
                		GameResourcePackages grp
                		JOIN GameResourcePackageResources grpr ON grpr.GameResourcePackageId = grp.Id
                		JOIN GameStringTableEntries gste ON gste.GameResourcePackageResourceId = grpr.Id
                	ORDER BY
                		1 DESC,
                		2 COLLATE {platformFunctions.FileSystemSQliteCollation}	
                )
                SELECT DISTINCT
                	SignedKey,
                	Locale,
                	FIRST_VALUE(PackagePath) OVER (PARTITION BY Locale, SignedKey) PackagePath,
                	FIRST_VALUE(KeyType) OVER (PARTITION BY Locale, SignedKey) KeyType,
                	FIRST_VALUE(KeyGroup) OVER (PARTITION BY Locale, SignedKey) KeyGroup,
                	FIRST_VALUE(KeyFullInstance) OVER (PARTITION BY Locale, SignedKey) KeyFullInstance
                FROM
                	AllStringTableEntries
                WHERE
                    SignedKey IN ({string.Join(", ", lookUpLocalizedStringsMessage.LocKeys.Select(locKey => unchecked((int)locKey)))})
                """
            };
            if (lookUpLocalizedStringsMessage.Locales.Any())
                queryParts.Add
                (
                    $"""
                        AND Locale IN ({string.Join(", ", lookUpLocalizedStringsMessage.Locales)})
                    """
                );
            var lastPackagePath = string.Empty;
            DataBasePackedFile? dbpf = null;
            ResourceKey lastStblKey = default;
            StringTableModel? stbl = null;
            try
            {
                using var pbDbContext = await pbDbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                await foreach (var (signedKey, locale, packagePath, keyType, keyGroup, keyFullInsance) in pbDbContext.Database.SqlQueryRaw<ProcessLookUpLocalizedStringsMessageQueryRecord>(string.Join("\n", queryParts)).AsAsyncEnumerable().ConfigureAwait(false))
                {
                    if (lastPackagePath != packagePath)
                    {
                        if (dbpf is not null)
                        {
                            stbl = null;
                            await dbpf.DisposeAsync().ConfigureAwait(false);
                            dbpf = null;
                        }
                        var packageFile = new FileInfo(File.Exists(packagePath) ? packagePath : Path.Combine(settings.UserDataFolderPath, "Mods", packagePath));
                        if (packageFile.Exists)
                            dbpf = await DataBasePackedFile.FromPathAsync(Path.Combine(settings.UserDataFolderPath, "Mods", packagePath)).ConfigureAwait(false);
                        lastPackagePath = packagePath;
                    }
                    var stblKey = new ResourceKey(unchecked((ResourceType)(uint)keyType), unchecked((uint)keyGroup), unchecked((ulong)keyFullInsance));
                    if (dbpf is not null && (stbl is null || stblKey != lastStblKey))
                    {
                        stbl = await dbpf!.GetStringTableAsync(stblKey).ConfigureAwait(false);
                        lastStblKey = stblKey;
                    }
                    var locKey = unchecked((uint)signedKey);
                    if (stbl is not null && stbl[locKey] is { } value && !string.IsNullOrWhiteSpace(value))
                        response.Entries.Add(new LookUpLocalizedStringsResponseEntry
                        {
                            Locale = locale,
                            LocKey = locKey,
                            Value = value
                        });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception");
            }
            finally
            {
                if (dbpf is not null)
                    await dbpf.DisposeAsync().ConfigureAwait(false);
            }
        }
        if (fromBridgedUiUniqueId is null)
            await SendMessageToProxyAsync(response).ConfigureAwait(false);
        else
            SendMessageToBridgedUis(response);
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task ProcessMessageAsync(JsonDocument message, JsonSerializerOptions jsonSerializerOptions, Guid? fromBridgedUiUniqueId = null)
    {
        var messageRoot = message.RootElement;
        var messageJson = messageRoot.ToString();
        if (!messageRoot.TryGetProperty("type", out var typeProperty)
            || typeProperty.ValueKind != JsonValueKind.String
            || typeProperty.GetString() is not { } typePropertyValue
            || !Enum.TryParse<ComponentMessageType>(typePropertyValue.Humanize().Dehumanize(), out var messageType))
        {
            logger.LogWarning("unrecognized message type: {Message}", messageJson);
            return;
        }
        switch (messageType)
        {
            case ComponentMessageType.Announcement:
                if (!messageRoot.TryGetProperty("announcement", out var announcement) || fromBridgedUiUniqueId is not { } announcerUniqueId)
                    return;
                var dynamicAnnouncement = JsonSerializer.Deserialize<dynamic>(announcement.GetRawText(), jsonSerializerOptions);
                await SendMessageToProxyAsync(new
                {
                    Type = nameof(HostMessageType.BridgedUiAnnouncement).Underscore().ToLowerInvariant(),
                    UniqueId = announcerUniqueId.ToString("n").ToLowerInvariant(),
                    Announcement = dynamicAnnouncement
                }).ConfigureAwait(false);
                SendMessageToBridgedUis(new
                {
                    Type = nameof(HostMessageType.BridgedUiAnnouncement).Camelize(),
                    UniqueId = announcerUniqueId.ToString("n").ToLowerInvariant(),
                    Announcement = dynamicAnnouncement
                });
                break;
            case ComponentMessageType.BridgedUiDomLoaded:
                if (fromBridgedUiUniqueId is not { } loaderUniqueId)
                    return;
                BridgedUiDomLoaded?.Invoke(this, new BridgedUiEventArgs{ UniqueId = loaderUniqueId });
                break;
            case ComponentMessageType.BridgedUiLookUp:
                if (TryParseMessage<BridgedUiLookUpMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "bridged UI look up", out var bridgedUiLookUpMessage))
                    await ProcessBridgedUiLookUpAsync(bridgedUiLookUpMessage).ConfigureAwait(false);
                break;
            case ComponentMessageType.BridgedUiRequest:
                if (TryParseMessage<BridgedUiRequestMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "bridged UI request", out var bridgedUiRequestMessage))
                    await ProcessBridgedUiRequestAsync(bridgedUiRequestMessage).ConfigureAwait(false);
                break;
            case ComponentMessageType.CloseBridgedUi:
                if (TryParseMessage<BridgedUiCloseMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "bridged UI close command", out var closeBridgedUiMessage))
                    await DestroyBridgedUiAsync(closeBridgedUiMessage.UniqueId).ConfigureAwait(false);
                break;
            case ComponentMessageType.FocusBridgedUi:
                if (TryParseMessage<FocusBridgedUiMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "bridged UI focus request", out var focusBridgedUiMessage))
                {
                    var success = loadedBridgedUis.ContainsKey(focusBridgedUiMessage.UniqueId);
                    if (success)
                        BridgedUiFocusRequested?.Invoke(this, new() { UniqueId = focusBridgedUiMessage.UniqueId });
                    await SendMessageToProxyAsync(new FocusBridgedUiResponseMessage
                    {
                        Success = success,
                        Type = nameof(HostMessageType.FocusBridgedUiResponse).Underscore().ToLowerInvariant(),
                        UniqueId = focusBridgedUiMessage.UniqueId
                    });
                }
                break;
            case ComponentMessageType.ForegroundGame:
                await platformFunctions.ForegroundGameAsync(new(settings.InstallationFolderPath)).ConfigureAwait(false);
                break;
            case ComponentMessageType.GameServiceEvent:
                if (TryParseMessage<GameServiceEventMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "game service event", out var gameServiceEventMessage)
                    && Enum.TryParse<GameServiceEvent>(gameServiceEventMessage.Event.Pascalize(), out var gameServiceEvent))
                {
                    if (gameServiceEvent is GameServiceEvent.Load)
                    {
                        gameIsSavingManualResetEvent.Set();
                        reserveSavesAccessManualResetEvent.Set();
                        _ = Task.Run(ResetSaveSpecificRelationalDataStorageAsync);
                        gameServicesRunningManualResetEvent.Set();
                    }
                    else if (gameServiceEvent is GameServiceEvent.PreSave)
                    {
                        gameIsSavingManualResetEvent.Reset();
                    }
                    else if (gameServiceEvent is GameServiceEvent.Save)
                        gameIsSavingManualResetEvent.Set();
                    else if (gameServiceEvent is GameServiceEvent.Stop)
                        gameServicesRunningManualResetEvent.Reset();
                    if (gameServiceEvent is GameServiceEvent.Load or GameServiceEvent.PreSave
                        && gameServiceEventMessage.NucleusId is { } gameServiceEventNucleusId
                        && gameServiceEventMessage.Created is { } gameServiceEventCreated
                        && gameServiceEventMessage.SimNow is { } gameServiceEventSimNow
                        && gameServiceEventMessage.SlotId is { } gameServiceEventSlotId)
                    {
                        saveNucleusId = gameServiceEventNucleusId;
                        saveCreated = gameServiceEventCreated;
                        saveSimNow = gameServiceEventSimNow;
                        saveSlotId = gameServiceEventSlotId;
                        if (gameServiceEvent is GameServiceEvent.PreSave)
                        {
                            saveSpecificDataStorageSnapshotNucleusId = saveNucleusId;
                            saveSpecificDataStorageSnapshotCreated = saveCreated;
                            saveSpecificDataStorageSnapshotSimNow = saveSimNow;
                            saveSpecificDataStorageSnapshotSlotId = saveSlotId;
                            saveSpecificDataStorageSnapshot = Task.Run(async () => await SnapshotSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false));
                            ConnectToSavesFolder();
                        }
                    }
                }
                break;
            case ComponentMessageType.ListScreenshots when fromBridgedUiUniqueId is { } bridgedUiRequestingScreenshotsList:
                var listScreenshotsResponseMessage = new ListScreenshotsResponseMessage()
                {
                    Type = nameof(HostMessageType.ListScreenshotsResponse).Camelize()
                };
                var screenshotsFolder = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "Screenshots"));
                if (screenshotsFolder.Exists)
                    foreach (var pngFile in screenshotsFolder.GetFiles("*.png", SearchOption.TopDirectoryOnly))
                        listScreenshotsResponseMessage.Screenshots.Add(new()
                        {
                            Attributes = pngFile.Attributes,
                            CreationTime = pngFile.CreationTime,
                            CreationTimeUtc = pngFile.CreationTimeUtc,
                            LastAccessTime = pngFile.LastAccessTime,
                            LastAccessTimeUtc = pngFile.LastAccessTimeUtc,
                            LastWriteTime = pngFile.LastWriteTime,
                            LastWriteTimeUtc = pngFile.LastWriteTimeUtc,
                            Name = pngFile.Name,
                            Size = pngFile.Length,
                            UnixFileMode = pngFile.UnixFileMode
                        });
                SpecificBridgedUiMessageSent?.Invoke(this, new()
                {
                    UniqueId = bridgedUiRequestingScreenshotsList,
                    MessageJson = JsonSerializer.Serialize(listScreenshotsResponseMessage, bridgedUiJsonSerializerOptions)
                });
                break;
            case ComponentMessageType.LookUpLocalizedStrings:
                if (TryParseMessage<LookUpLocalizedStringsMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "look up localized strings", out var lookUpLocalizedStringsMessage))
                    _ = Task.Run(async () => await ProcessLookUpLocalizedStringsMessageAsync(lookUpLocalizedStringsMessage, fromBridgedUiUniqueId).ConfigureAwait(false));
                break;
            case ComponentMessageType.OpenUrl:
                if (TryParseMessage<OpenUrlMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "open URL", out var openUrlMessage)
                    && openUrlMessage.Url is { } openUrlMessageUrl)
                    StaticDispatcher.Dispatch(() => Browser.OpenAsync(openUrlMessageUrl, BrowserLaunchMode.External));
                break;
            case ComponentMessageType.QueryRelationalDataStorage:
                if (TryParseMessage<QueryRelationalDataStorageMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "query relational data storage", out var queryRelationalDataStorageMessage))
                    _ = Task.Run(() => ProcessQueryRelationalDataStorageMessageAsync(queryRelationalDataStorageMessage));
                break;
            case ComponentMessageType.SendDataToBridgedUi:
                if (!messageRoot.TryGetProperty("recipient", out var recipientProperty)
                    || recipientProperty.GetString() is not { } recipientPropertyValue
                    || string.IsNullOrWhiteSpace(recipientPropertyValue)
                    || !Guid.TryParse(recipientPropertyValue, out var recipient)
                    || !messageRoot.TryGetProperty("data", out var data))
                    return;
                BridgedUiDataSent?.Invoke(this, new()
                {
                    Recipient = recipient,
                    MessageJson = JsonSerializer.Serialize(new
                    {
                        Type = nameof(HostMessageType.BridgedUiData).Camelize(),
                        Data = JsonSerializer.Deserialize<dynamic>(data.GetRawText(), jsonSerializerOptions)
                    }, bridgedUiJsonSerializerOptions)
                });
                break;
            case ComponentMessageType.SendLoadedSaveIdentifiersResponse:
                if (TryParseMessage<SendLoadedSaveIdentifiersResponseMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "send loaded save identifiers response", out var sendLoadedSaveIdentifiersResponseMessage)
                    && sendLoadedSaveIdentifiersResponseMessage.NucleusId is { } requestedNucleusId
                    && sendLoadedSaveIdentifiersResponseMessage.Created is { } requestedCreated
                    && sendLoadedSaveIdentifiersResponseMessage.SimNow is { } requestedSimNow
                    && sendLoadedSaveIdentifiersResponseMessage.SlotId is { } requestedSlotId)
                {
                    saveNucleusId = requestedNucleusId;
                    saveCreated = requestedCreated;
                    saveSimNow = requestedSimNow;
                    saveSlotId = requestedSlotId;
                    gameServicesRunningManualResetEvent.Set();
                }
                break;
        }
    }

    public async Task ProcessMessageFromBridgedUiAsync(Guid uniqueId, string messageJson)
    {
        logger.LogDebug("message received from bridged UI {UniqueId}: {Message}", uniqueId, messageJson);
        JsonDocument message;
        try
        {
            message = JsonDocument.Parse(messageJson);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "unparsable message from bridged UI {UniqueId}: {Message}", uniqueId, messageJson);
            return;
        }
        await ProcessMessageAsync(message, bridgedUiJsonSerializerOptions, uniqueId).ConfigureAwait(false);
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task ProcessPathsQueueAsync()
    {
        if (this.pathsProcessingQueue is not { } pathsProcessingQueue)
            return;
        reserveSavesAccessManualResetEvent.Reset();
        saveSpecificDataStorageProcessingIdentifiersDenyList.Clear();
        foreach (var backupFile in new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"))
            .GetFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(file => GetBackupSaveFileExtensionPattern().IsMatch(file.Extension)))
        {
            try
            {
                using var savePackage = await DataBasePackedFile.FromPathAsync(backupFile.FullName).ConfigureAwait(false);
                var packageKeys = await savePackage.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                if (saveGameDataKeys.Length is <= 0 or >= 2)
                    continue;
                var saveGameDataKey = saveGameDataKeys[0];
                var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await savePackage.GetAsync(saveGameDataKey, cancellationToken: cancellationToken).ConfigureAwait(false));
                if (saveGameData is null)
                    continue;
                if (saveGameData.Account is not { } account
                    || saveGameData.SaveSlot is not { } saveSlot
                    || saveSlot.GameplayData is not { } gamePlayData)
                    continue;
                saveSpecificDataStorageProcessingIdentifiersDenyList.AddOrUpdate((account.NucleusId, account.Created, gamePlayData.WorldGameTime), true, (_, _) => true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception while processing {Path} for deny list scan", backupFile.FullName);
            }
        }
        reserveSavesAccessManualResetEvent.Set();
        var filesProcessedInCycle = new HashSet<string>(platformFunctions.FileSystemStringComparer);
        var gameWasSaving = false;
        Process? gameProcess = null;
        while (await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            reserveSavesAccessManualResetEvent.Reset();
            string? path = null;
            try
            {
                gameProcess ??= await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath));
                if (gameProcess is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    continue;
                }
                if (!gameWasSaving)
                    gameWasSaving = gameIsSavingManualResetEvent.IsSet;
                path = await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false);
                if (!filesProcessedInCycle.Add(path))
                    continue;
                var file = new FileInfo(path);
                var isBackupSaveFile = GetBackupSaveFileExtensionPattern().IsMatch(file.Extension);
                var isSaveFile = file.Extension.Equals(".save", StringComparison.OrdinalIgnoreCase);
                if (!isBackupSaveFile && !isSaveFile)
                    continue;
                if (!file.Exists)
                    continue;
                await Task.WhenAll(gameIsSavingManualResetEvent.WaitAsync(), Task.Delay(oneQuarterSecond)).ConfigureAwait(false);
                var length = file.Length;
                while (true)
                {
                    await Task.Delay(oneQuarterSecond).ConfigureAwait(false);
                    file.Refresh();
                    var newLength = file.Length;
                    if (length == newLength)
                        break;
                    length = newLength;
                }
                if (file.LastWriteTime < gameProcess.StartTime)
                    continue;
                using var savePackage = await DataBasePackedFile.FromPathAsync(file.FullName, forReadOnly: false).ConfigureAwait(false);
                var packageKeys = await savePackage.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                if (saveGameDataKeys.Length is <= 0 or >= 2)
                    continue;
                var saveGameDataKey = saveGameDataKeys[0];
                var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await savePackage.GetAsync(saveGameDataKey, cancellationToken: cancellationToken).ConfigureAwait(false));
                if (saveGameData is null)
                    continue;
                if (saveGameData.Account is not { } account
                    || account.NucleusId != saveSpecificDataStorageSnapshotNucleusId
                    || account.Created != saveSpecificDataStorageSnapshotCreated
                    || saveGameData.SaveSlot is not { } saveSlot
                    || saveSpecificDataStorageSnapshotSlotId is not 0
                    && saveSlot.SlotId != saveSpecificDataStorageSnapshotSlotId
                    || saveSlot.GameplayData is not { } gamePlayData
                    || gamePlayData.WorldGameTime > saveSpecificDataStorageSnapshotSimNow)
                    continue;
                if (isBackupSaveFile)
                {
                    saveSpecificDataStorageProcessingIdentifiersDenyList.AddOrUpdate((account.NucleusId, account.Created, gamePlayData.WorldGameTime), true, (_, _) => true);
                    continue;
                }
                if (!gameWasSaving
                    && saveSpecificDataStorageProcessingIdentifiersDenyList.ContainsKey((account.NucleusId, account.Created, gamePlayData.WorldGameTime)))
                    continue;
                if (saveSpecificDataStorageSnapshot is null)
                    continue;
                var (saveSpecificDataKey, saveSpecificDataContent) = await saveSpecificDataStorageSnapshot.ConfigureAwait(false);
                if (saveSpecificDataContent.IsEmpty)
                    continue;
                if (packageKeys.Contains(saveSpecificDataKey)
                    && (await savePackage.GetAsync(saveSpecificDataKey).ConfigureAwait(false)).Span.SequenceEqual(saveSpecificDataContent.Span))
                    continue;
                foreach (var key in packageKeys.Where(pk => pk.Type is ResourceType.SaveSpecificRelationalDataStorage))
                    savePackage.Delete(key);
                await savePackage.SetAsync(saveSpecificDataKey, saveSpecificDataContent, CompressionMode.ForceZLib).ConfigureAwait(false);
                await savePackage.SaveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception while processing {Path}", path);
            }
            finally
            {
                var outputAvailable = false;
                try
                {
                    outputAvailable = await pathsProcessingQueue.OutputAvailableAsync(new CancellationToken(true)).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                if (!outputAvailable)
                {
                    filesProcessedInCycle.Clear();
                    gameWasSaving = false;
                    reserveSavesAccessManualResetEvent.Set();
                }
            }
        }
        saveSpecificDataStorageProcessingIdentifiersDenyList.Clear();
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    [SuppressMessage("Security", "CA2100: Review SQL queries for security vulnerabilities", Justification = "That's on modders. We give them parameters.")]
    async Task ProcessQueryRelationalDataStorageMessageAsync(QueryRelationalDataStorageMessage queryRelationalDataStorageMessage)
    {
        var errorCode = 0;
        string? errorMessage = null;
        var extendedErrorCode = 0;
        var recordSets = new List<RelationalDataStorageQueryRecordSet>();
        var executionStopwatch = new Stopwatch();
        try
        {
            await WithRelationalDataStorageConnectionAsync(queryRelationalDataStorageMessage.UniqueId, queryRelationalDataStorageMessage.IsSaveSpecific, async conn =>
            {
                try
                {
                    var com = conn.CreateCommand();
                    com.CommandText = queryRelationalDataStorageMessage.Query;
                    if (queryRelationalDataStorageMessage.Parameters?.Any() ?? false)
                        foreach (var (key, value) in queryRelationalDataStorageMessage.Parameters)
                        {
                            if (value is JsonElement jsonValue)
                            {
                                if (jsonValue.ValueKind is JsonValueKind.Null)
                                    com.Parameters.AddWithValue(key, DBNull.Value);
                                else if (jsonValue.ValueKind is JsonValueKind.False)
                                    com.Parameters.AddWithValue(key, 0);
                                else if (jsonValue.ValueKind is JsonValueKind.True)
                                    com.Parameters.AddWithValue(key, 1);
                                else if (jsonValue.ValueKind is JsonValueKind.String)
                                    com.Parameters.AddWithValue(key, jsonValue.GetString());
                                else if (jsonValue.ValueKind is JsonValueKind.Number)
                                {
                                    if (jsonValue.TryGetInt64(out var int64))
                                        com.Parameters.AddWithValue(key, int64);
                                    else if (jsonValue.TryGetDouble(out var dbl))
                                        com.Parameters.AddWithValue(key, dbl);
                                }
                                else if (jsonValue.ValueKind is JsonValueKind.Object
                                    && jsonValue.TryGetProperty("base64", out var base64Property)
                                    && base64Property.ValueKind is JsonValueKind.String
                                    && base64Property.GetString() is { } base64)
                                    com.Parameters.AddWithValue(key, Convert.FromBase64String(base64));
                                else
                                    throw new NotSupportedException($"unsupported value kind: {jsonValue.ValueKind}");
                                continue;
                            }
                            com.Parameters.AddWithValue(key, value ?? DBNull.Value);
                        }
                    executionStopwatch.Start();
                    await using var reader = await com.ExecuteReaderAsync().ConfigureAwait(false);
                    do
                    {
                        var recordSet = new RelationalDataStorageQueryRecordSet();
                        for (var i = 0; i < reader.FieldCount; ++i)
                            recordSet.FieldNames.Add(reader.GetName(i));
                        while (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            var record = new List<object?>();
                            for (var i = 0; i < reader.FieldCount; ++i)
                            {
                                var value = reader.GetValue(i);
                                if (value == DBNull.Value)
                                    value = null;
                                else if (value is IEnumerable<byte> bytes)
                                    value = new { base64 = Convert.ToBase64String(bytes is byte[] bytesArray ? bytesArray : [.. bytes]) };
                                record.Add(value);
                            }
                            recordSet.Records.Add(record);
                        }
                        recordSets.Add(recordSet);
                    } while (await reader.NextResultAsync().ConfigureAwait(false));
                }
                catch (SqliteException sqlEx)
                {
                    errorCode = sqlEx.SqliteErrorCode;
                    extendedErrorCode = sqlEx.SqliteExtendedErrorCode;
                    errorMessage = sqlEx.Message;
                }
                finally
                {
                    executionStopwatch.Stop();
                }
            }, new CancellationToken(queryRelationalDataStorageMessage.IsSaveSpecific)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            errorCode = -2;
            extendedErrorCode = -2;
            errorMessage = "Your query is very important to us, but the player is currently in the Main Menu, Manage Worlds, or CAS. Please try your query again later.";
        }
        catch (Exception ex)
        {
            errorCode = -1;
            extendedErrorCode = -1;
            errorMessage = ex.Message;
        }
        var proxyResults = new RelationalDataStorageQueryResultsMessage
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ExecutionSeconds = executionStopwatch.Elapsed.TotalSeconds,
            ExtendedErrorCode = extendedErrorCode,
            IsSaveSpecific = queryRelationalDataStorageMessage.IsSaveSpecific,
            QueryId = queryRelationalDataStorageMessage.QueryId,
            Tag = queryRelationalDataStorageMessage.Tag,
            Type = nameof(HostMessageType.RelationalDataStorageQueryResults).Underscore().ToLowerInvariant(),
            UniqueId = queryRelationalDataStorageMessage.UniqueId
        };
        var bridgedUiResults = new RelationalDataStorageQueryResultsMessage
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ExecutionSeconds = executionStopwatch.Elapsed.TotalSeconds,
            ExtendedErrorCode = extendedErrorCode,
            IsSaveSpecific = queryRelationalDataStorageMessage.IsSaveSpecific,
            QueryId = queryRelationalDataStorageMessage.QueryId,
            Tag = queryRelationalDataStorageMessage.Tag,
            Type = nameof(HostMessageType.RelationalDataStorageQueryResults).Camelize(),
            UniqueId = queryRelationalDataStorageMessage.UniqueId
        };
        foreach (var recordSet in recordSets)
        {
            proxyResults.RecordSets.Add(recordSet);
            bridgedUiResults.RecordSets.Add(recordSet);
        }
        await SendMessageToProxyAsync(proxyResults).ConfigureAwait(false);
        SendMessageToBridgedUis(bridgedUiResults);
    }

    void PromptPlayerForBridgedUiAuthorization(BridgedUiRequestMessage request, ZipFile? archive = null)
    {
        var playerResponseTaskCompletionSource = new TaskCompletionSource<bool>();
        BridgedUiRequested?.Invoke(this, new(playerResponseTaskCompletionSource)
        {
            RequestorName = request.RequestorName,
            RequestReason = request.RequestReason,
            TabName = request.TabName
        });
        _ = Task.Run(async () =>
        {
            var isAuthorized = await playerResponseTaskCompletionSource.Task.ConfigureAwait(false);
            if (connectedClients.IsEmpty)
                return;
            if (!isAuthorized)
            {
                if (archive is IDisposable disposableArchive)
                    disposableArchive.Dispose();
                await BridgedUiRequestResolvedAsync(request.UniqueId, BridgedUiRequestResponseMessage.DenialReason_PlayerDeniedRequest).ConfigureAwait(false);
                return;
            }
            loadedBridgedUis.AddOrUpdate(request.UniqueId, archive, (k, v) => v);
            await BridgedUiRequestResolvedAsync(request.UniqueId, BridgedUiRequestResponseMessage.DenialReason_None).ConfigureAwait(false);
            BridgedUiAuthorized?.Invoke(this, new()
            {
                Archive = archive,
                HostName = request.HostName,
                TabIconPath = request.TabIconPath,
                TabName = request.TabName,
                UiRoot = request.UiRoot,
                UniqueId = request.UniqueId
            });
        });
    }

    async Task ResetSaveSpecificRelationalDataStorageAsync()
    {
        using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.WriterLockAsync(cancellationToken).ConfigureAwait(false);
        foreach (var uniqueId in saveSpecificDataStorageConnections.Keys)
            if (saveSpecificDataStorageConnections.TryRemove(uniqueId, out var connection))
                await connection.DisposeAsync().ConfigureAwait(false);
        pendingSaveSpecificDataStorageInitialization = true;
    }

    void SendMessageToBridgedUis(object message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        BridgedUiMessageSent?.Invoke(this, new()
        {
            MessageJson = JsonSerializer.Serialize(message, bridgedUiJsonSerializerOptions)
        });
    }

    async Task SendMessageToProxyAsync(object message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        if (connectedClients.IsEmpty)
            return;
        var serializedMessageBytes = JsonSerializer.SerializeToUtf8Bytes(message, proxyJsonSerializerOptions);
        Memory<byte> serializedMessageSizeBytes = new byte[4];
        var serializedMessageSize = BinaryPrimitives.ReverseEndianness(serializedMessageBytes.Length);
        MemoryMarshal.Write(serializedMessageSizeBytes.Span, in serializedMessageSize);
        foreach (var (client, clientWriteLock) in connectedClients)
        {
            try
            {
                using var clientWriteLockHeld = await clientWriteLock.LockAsync(cancellationToken).ConfigureAwait(false);
                var stream = client.GetStream();
                await stream.WriteAsync(serializedMessageSizeBytes, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(serializedMessageBytes, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ProxyDisconnected?.Invoke(this, EventArgs.Empty);
                logger.LogError(ex, "unexpected unhandled exception while processing proxy communication");
                if (connectedClients.TryRemove(client, out var finallyClientWriteLock))
                {
                    using var clientWriteLockHeld = await finallyClientWriteLock.LockAsync(cancellationToken).ConfigureAwait(false);
                    IsClientConnected = !connectedClients.IsEmpty;
                    client.Dispose();
                }
            }
        }
    }

    [SuppressMessage("Security", "CA2100: Review SQL queries for security vulnerabilities")]
    async Task<(ResourceKey key, ReadOnlyMemory<byte> content)> SnapshotSaveSpecificRelationalDataStorageAsync(CancellationToken cancellationToken = default)
    {
        using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.WriterLockAsync(cancellationToken).ConfigureAwait(false);
        if (saveSpecificDataStorageConnections.IsEmpty)
            return (new(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 0), ReadOnlyMemory<byte>.Empty);
        using var zipFileStream = new ArrayBufferWriterOfByteStream();
        using var zipOutputStream = new ZipOutputStream(zipFileStream) { IsStreamOwner = false };
        zipOutputStream.SetLevel(0);
        foreach (var (uniqueId, connection) in saveSpecificDataStorageConnections)
        {
            var rawSerializedDatabase = raw.sqlite3_serialize(connection.Handle, "main", out var serializedDatabaseSize, 0);
            var serializedDatabase = ArrayPool<byte>.Shared.Rent((int)serializedDatabaseSize);
            Marshal.Copy(rawSerializedDatabase, serializedDatabase, 0, (int)serializedDatabaseSize);
            raw.sqlite3_free(rawSerializedDatabase);
            await zipOutputStream.PutNextEntryAsync(new ZipEntry($"{uniqueId:n}.sqlite")
            {
                DateTime = DateTime.Now,
                Size = serializedDatabaseSize
            }, CancellationToken.None).ConfigureAwait(false);
            ReadOnlyMemory<byte> serializedDatabaseMemory = serializedDatabase;
            await zipOutputStream.WriteAsync(serializedDatabaseMemory[..(int)serializedDatabaseSize], CancellationToken.None).ConfigureAwait(false);
            await zipOutputStream.CloseEntryAsync(CancellationToken.None).ConfigureAwait(false);
            ArrayPool<byte>.Shared.Return(serializedDatabase);
        }
        await zipOutputStream.FinishAsync(CancellationToken.None).ConfigureAwait(false);
        return (new(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 0), zipFileStream.WrittenMemory);
    }

    public Task WaitForSavesAccessAsync(CancellationToken cancellationToken = default) =>
        reserveSavesAccessManualResetEvent.WaitAsync(cancellationToken);

    async Task WithRelationalDataStorageConnectionAsync(Guid uniqueId, bool isSaveSpecific, Func<SqliteConnection, Task> withSqliteConnectionAsyncAction, CancellationToken cancellationToken = default)
    {
        if (isSaveSpecific)
        {
            await gameServicesRunningManualResetEvent.WaitAsync(new(true));
            using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
            await InitializeSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false);
            using var dataStorageConnectionLockHeld = await saveSpecificDataStorageConnectionLocks.GetOrAdd(uniqueId, _ => new()).LockAsync(CancellationToken.None).ConfigureAwait(false);
            var saveSpecificConnection = saveSpecificDataStorageConnections.GetOrAdd(uniqueId, _ => new SqliteConnection("Data Source=:memory:"));
            if (saveSpecificConnection.State is not ConnectionState.Open)
                await saveSpecificConnection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await withSqliteConnectionAsyncAction(saveSpecificConnection).ConfigureAwait(false);
            return;
        }
        var appDataDirectory = await GetAppDataDirectoryAsync().ConfigureAwait(false);
        var databaseDirectoryPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
        var databaseFile = new FileInfo(Path.Combine(databaseDirectoryPath, $"{uniqueId:n}.sqlite"));
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile.FullName,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = !isSaveSpecific
        };
        using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
        await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
        if (!isSaveSpecific)
        {
            var enableWriteAheadLoggingForGlobalDatabases = connection.CreateCommand();
            enableWriteAheadLoggingForGlobalDatabases.CommandText = "PRAGMA journal_mode=WAL;";
            await enableWriteAheadLoggingForGlobalDatabases.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }
        await withSqliteConnectionAsyncAction(connection).ConfigureAwait(false);
        await connection.CloseAsync().ConfigureAwait(false);
    }

    record ProcessLookUpLocalizedStringsMessageQueryRecord(int SignedKey, byte Locale, string PackagePath, int KeyType, int KeyGroup, long KeyFullInstance);
}
