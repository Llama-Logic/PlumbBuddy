using AngleSharp.Common;
using SQLitePCL;
using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services;

[SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "This is for Python, CA. You have your head up your ass.")]
[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class ProxyHost :
    IProxyHost
{
    record ProcessLookUpLocalizedStringsMessageQueryRecord(int SignedKey, byte Locale, string PackagePath, int KeyType, int KeyGroup, long KeyFullInstance);

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
    static readonly ReadOnlyMemory<byte> pngSignature = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
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
    static readonly ReadOnlyMemory<byte> screenshotPngCustomChunkType = new byte[] { 0x70, 0x72, 0x4f, 0x50 }; // prOP

    static JsonSerializerOptions AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new PermissiveGuidConverter());
        return options;
    }

    static GamepadConnectedMessage CreateGamepadConnectedMessage(IObservableGamepad gamepad, int index, bool python) =>
        new()
        {
            Index = index,
            Name = gamepad.Name,
            Buttons = gamepad.Buttons.Select(button => new List<object>([button.Name, button.Pressed]).AsReadOnly()).ToList().AsReadOnly(),
            Thumbsticks = gamepad.Thumbsticks.Select(thumbstick => new List<object>([thumbstick.X, thumbstick.Y, thumbstick.Direction, thumbstick.Position]).AsReadOnly()).ToList().AsReadOnly(),
            Triggers = gamepad.Triggers.Select(trigger => trigger.Position).ToList().AsReadOnly(),
            Type = python
                ? nameof(HostMessageType.GamepadConnected).Underscore().ToLowerInvariant()
                : nameof(HostMessageType.GamepadConnected).Camelize()
        };

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

    public ProxyHost(IPlatformFunctions platformFunctions, ILogger<ProxyHost> logger, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IGameResourceCataloger gameResourceCataloger, IGamepadInterop gamepadInterop, IDesktopInputInterceptor desktopInputInterceptor)
    {
        ArgumentNullException.ThrowIfNull(platformFunctions);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(gameResourceCataloger);
        ArgumentNullException.ThrowIfNull(gamepadInterop);
        ArgumentNullException.ThrowIfNull(desktopInputInterceptor);
        this.platformFunctions = platformFunctions;
        this.logger = logger;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.gameResourceCataloger = gameResourceCataloger;
        this.gamepadInterop = gamepadInterop;
        this.desktopInputInterceptor = desktopInputInterceptor;
        cancellationTokenSource = new();
        cancellationToken = cancellationTokenSource.Token;
        connectedClients = new();
        bridgedUiLoadingLocks = new();
        loadedBridgedUis = new();
        desktopInputInterceptorLock = new();
        desktopInputInterceptorReferenceCounts = [];
        desktopInputMessagingQueue = new();
        gameIsSavingManualResetEvent = new(true);
        gamepadMessagingEnrolledBridgedUis = new();
        gamepadMessagingEnrolledClients = new();
        reserveSavesAccessManualResetEvent = new(true);
        gameServicesRunningManualResetEvent = new(false);
        saveSpecificDataStorageConnectionLocks = new();
        saveSpecificDataStorageConnections = new();
        saveSpecificDataStorageInitializationLock = new();
        saveSpecificDataStorageProcessingIdentifiersDenyList = new();
        saveSpecificDataStoragePropagationLock = new();
        pendingSaveSpecificDataStorageInitialization = true;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        wiredGamepadsLock = new();
        wiredGamepads = [];
        foreach (var gamepad in this.gamepadInterop.Gamepads)
            WireGamepad(gamepad);
        ((INotifyCollectionChanged)this.gamepadInterop.Gamepads).CollectionChanged += HandleGamepadInteropGamepadsCollectionChanged;
        desktopInputInterceptor.KeyDown += HandleDesktopInputInterceptorKeyDown;
        desktopInputInterceptor.KeyUp += HandleDesktopInputInterceptorKeyUp;
        gamepadMessagingQueue = new();
        _ = Task.Run(ProcessDesktopInputMessagesAsync);
        _ = Task.Run(ProcessGamepadMessagesAsync);
        listener = new(IPAddress.Loopback, port);
        listener.Start();
        _ = Task.Run(ListenAsync);
    }

    ~ProxyHost() =>
        Dispose(false);

    readonly ConcurrentDictionary<Guid, AsyncLock> bridgedUiLoadingLocks;
    readonly CancellationTokenSource cancellationTokenSource;
    readonly CancellationToken cancellationToken;
    readonly ConcurrentDictionary<TcpClient, AsyncLock> connectedClients;
    readonly IDesktopInputInterceptor desktopInputInterceptor;
    readonly AsyncLock desktopInputInterceptorLock;
    readonly Dictionary<DesktopInputKey, int> desktopInputInterceptorReferenceCounts;
    readonly AsyncProducerConsumerQueue<(DesktopInputKey key, bool isDown)> desktopInputMessagingQueue;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "CA can't tell that this is actually happening")]
    FileSystemWatcher? fileSystemWatcher;
    readonly IGamepadInterop gamepadInterop;
    readonly IGameResourceCataloger gameResourceCataloger;
    bool isBridgedUiDevelopmentModeEnabled;
    bool isClientConnected;
    readonly TcpListener listener;
    readonly ConcurrentDictionary<Guid, IReadOnlyList<ZipFile>> loadedBridgedUis;
    readonly ILogger<ProxyHost> logger;
    AsyncProducerConsumerQueue<string>? pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly AsyncManualResetEvent gameIsSavingManualResetEvent;
    readonly ConcurrentDictionary<Guid, object?> gamepadMessagingEnrolledBridgedUis;
    readonly ConcurrentDictionary<TcpClient, object?> gamepadMessagingEnrolledClients;
    readonly AsyncProducerConsumerQueue<Func<Task>> gamepadMessagingQueue;
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
    readonly HashSet<IObservableGamepad> wiredGamepads;
    readonly AsyncLock wiredGamepadsLock;

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

    async Task<ZipFile?> CacheScriptModAsync(string modsDirectoryRelativePath, Guid uniqueId, int layerIndex)
    {
        var path = Path.Combine(settings.UserDataFolderPath, "Mods", modsDirectoryRelativePath);
        if (!File.Exists(path))
            return null;
        var cacheDirectory = await GetCacheDirectoryAsync().ConfigureAwait(false);
        var cachePath = Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs");
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);
        cachePath = Path.Combine(cachePath, $"{uniqueId:n}-{layerIndex}.ts4script");
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

    IReadOnlyList<GamepadsResetMessageGamepad> CreateGamepadsResetMessageGamepadList() =>
        gamepadInterop.Gamepads.Select(gamepad => new GamepadsResetMessageGamepad
        {
            Name = gamepad.Name,
            Buttons = gamepad.Buttons.Select(button => new List<object>([button.Name, button.Pressed]).AsReadOnly()).ToList().AsReadOnly(),
            Thumbsticks = gamepad.Thumbsticks.Select(thumbstick => new List<object>([thumbstick.X, thumbstick.Y, thumbstick.Direction, thumbstick.Position]).AsReadOnly()).ToList().AsReadOnly(),
            Triggers = gamepad.Triggers.Select(trigger => trigger.Position).ToList().AsReadOnly()
        }).ToList().AsReadOnly();

    public void DestroyBridgedUi(Guid uniqueId) =>
        _ = Task.Run(async () => await DestroyBridgedUiAsync(uniqueId).ConfigureAwait(false));

    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope")]
    async Task DestroyBridgedUiAsync(Guid uniqueId)
    {
        using var bridgedUiLoadingLockHeld = await bridgedUiLoadingLocks.GetOrAdd(uniqueId, _ => new()).LockAsync().ConfigureAwait(false);
        if (!loadedBridgedUis.TryRemove(uniqueId, out var archives))
            return;
        logger.LogDebug("I now take from bridged UI {UniqueId} its power, in the name of CodeBleu, and Scumbumbo before. I, PlumbBuddy, CAST YOU OFF!", uniqueId);
        gamepadMessagingEnrolledBridgedUis.TryRemove(uniqueId, out _);
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
        foreach (var achive in archives)
        {
            achive.Close();
            if (achive is IDisposable disposableArchive)
                disposableArchive.Dispose();
        }
        var cacheDirectory = await GetCacheDirectoryAsync().ConfigureAwait(false);
        var uiBridgeTabIconDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "UI Bridge Icons", $"{uniqueId:n}"));
        if (uiBridgeTabIconDirectory.Exists)
            uiBridgeTabIconDirectory.Delete(true);
        var uiBridgeTabsDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs"));
        foreach (var cachedArchive in uiBridgeTabsDirectory.GetFiles($"{uniqueId:n}-*.ts4script"))
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
            desktopInputInterceptor.KeyDown -= HandleDesktopInputInterceptorKeyDown;
            desktopInputInterceptor.KeyUp -= HandleDesktopInputInterceptorKeyUp;
            desktopInputMessagingQueue.CompleteAdding();
            ((INotifyCollectionChanged)gamepadInterop.Gamepads).CollectionChanged -= HandleGamepadInteropGamepadsCollectionChanged;
            gamepadMessagingQueue.CompleteAdding();
            settings.PropertyChanged -= HandleSettingsPropertyChanged;
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
            gamepadMessagingEnrolledClients.TryRemove(client, out _);
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

    public Task ForegroundPlumbBuddyAsync(bool pauseGame = false) =>
        SendMessageToProxyAsync(new ForegroundPlumbBuddyMessage
        {
            PauseGame = pauseGame,
            Type = nameof(HostMessageType.ForegroundPlumbbuddy).Underscore().ToLowerInvariant()
        });

    async void HandleDesktopInputInterceptorKeyDown(object? sender, DesktopInputEventArgs e) =>
        desktopInputMessagingQueue.Enqueue((e.Key, true));

    async void HandleDesktopInputInterceptorKeyUp(object? sender, DesktopInputEventArgs e) =>
        desktopInputMessagingQueue.Enqueue((e.Key, false));

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

    void HandleGamepadButtonButtonUpdated(object? sender, ButtonUpdatedEventArgs e)
    {
        if (sender is not IObservableButton observableButton)
            return;
        var index = gamepadInterop.Gamepads.IndexOf(observableButton.Gamepad);
        var proxyMessage = new GamepadButtonChangedMessage
        {
            Index = index,
            Name = observableButton.Name,
            Pressed = e.Pressed,
            Type = nameof(HostMessageType.GamepadButtonChanged).Underscore().ToLowerInvariant()
        };
        var bridgedUiMessage = new GamepadButtonChangedMessage
        {
            Index = index,
            Name = observableButton.Name,
            Pressed = e.Pressed,
            Type = nameof(HostMessageType.GamepadButtonChanged).Camelize()
        };
        gamepadMessagingQueue.Enqueue(() => SendGamepadMessageAsync(proxyMessage, bridgedUiMessage));
    }

    void HandleGamepadInteropGamepadsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } newItems:
                var newGamepads = newItems.Cast<IObservableGamepad>();
                var proxyConnectedMessages = newGamepads.Select((gamepad, index) => CreateGamepadConnectedMessage(gamepad, e.NewStartingIndex + index, true)).ToImmutableArray();
                var bridgedUiConnectedMessages = newGamepads.Select((gamepad, index) => CreateGamepadConnectedMessage(gamepad, e.NewStartingIndex + index, false)).ToImmutableArray();
                gamepadMessagingQueue.Enqueue(async () =>
                {
                    for (var i = 0; i < proxyConnectedMessages.Length; ++i)
                        await SendGamepadMessageAsync(proxyConnectedMessages[i], bridgedUiConnectedMessages[i]).ConfigureAwait(false);
                });
                foreach (var newGamepad in newGamepads)
                    WireGamepad(newGamepad);
                break;
            case NotifyCollectionChangedAction.Move when e.OldItems is { } movedItems:
                var movedGamepads = movedItems.Cast<IObservableGamepad>();
                var proxyMovedMessages = movedGamepads.Select((_, index) => new GamepadMovedMessage
                {
                    Index = e.OldStartingIndex + index,
                    NewIndex = e.NewStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadMoved).Underscore().ToLowerInvariant()
                }).ToImmutableArray();
                var bridgedUiMovedMessages = movedGamepads.Select((_, index) => new GamepadMovedMessage
                {
                    Index = e.OldStartingIndex + index,
                    NewIndex = e.NewStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadMoved).Camelize()
                }).ToImmutableArray();
                gamepadMessagingQueue.Enqueue(async () =>
                {
                    for (var i = 0; i < proxyMovedMessages.Length; ++i)
                        await SendGamepadMessageAsync(proxyMovedMessages[i], bridgedUiMovedMessages[i]).ConfigureAwait(false);
                });
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is { } oldItems:
                var removedGamepads = oldItems.Cast<IObservableGamepad>();
                var proxyDisconnectedMessages = removedGamepads.Select((_, index) => new GamepadMessage
                {
                    Index = e.OldStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadDisconnected).Underscore().ToLowerInvariant()
                }).ToImmutableArray();
                var bridgedUiDisconnectedMessages = removedGamepads.Select((_, index) => new GamepadMessage
                {
                    Index = e.OldStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadDisconnected).Camelize()
                }).ToImmutableArray();
                gamepadMessagingQueue.Enqueue(async () =>
                {
                    for (var i = 0; i < proxyDisconnectedMessages.Length; ++i)
                        await SendGamepadMessageAsync(proxyDisconnectedMessages[i], bridgedUiDisconnectedMessages[i]).ConfigureAwait(false);
                });
                foreach (var newGamepad in removedGamepads)
                    UnwireGamepad(newGamepad);
                break;
            case NotifyCollectionChangedAction.Replace when e.OldItems is { } replacedItems && e.NewItems is { } replacedWithItems:
                var replacedGamepads = replacedItems.Cast<IObservableGamepad>();
                var proxyReplacedMessages = replacedGamepads.Select((_, index) => new GamepadMessage
                {
                    Index = e.OldStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadDisconnected).Underscore().ToLowerInvariant()
                }).ToImmutableArray();
                var bridgedUiReplacedMessages = replacedGamepads.Select((_, index) => new GamepadMessage
                {
                    Index = e.OldStartingIndex + index,
                    Type = nameof(HostMessageType.GamepadDisconnected).Camelize()
                }).ToImmutableArray();
                var replacedWithGamepads = replacedWithItems.Cast<IObservableGamepad>();
                var proxyReplacedWithMessages = replacedWithGamepads.Select((gamepad, index) => CreateGamepadConnectedMessage(gamepad, e.NewStartingIndex + index, true)).ToImmutableArray();
                var bridgedUiReplacedWithMessages = replacedWithGamepads.Select((gamepad, index) => CreateGamepadConnectedMessage(gamepad, e.NewStartingIndex + index, false)).ToImmutableArray();
                gamepadMessagingQueue.Enqueue(async () =>
                {
                    for (var i = 0; i < proxyReplacedMessages.Length; ++i)
                        await SendGamepadMessageAsync(proxyReplacedMessages[i], bridgedUiReplacedMessages[i]).ConfigureAwait(false);
                    for (var i = 0; i < proxyReplacedWithMessages.Length; ++i)
                        await SendGamepadMessageAsync(proxyReplacedWithMessages[i], bridgedUiReplacedWithMessages[i]).ConfigureAwait(false);
                });
                foreach (var newGamepad in replacedGamepads)
                    UnwireGamepad(newGamepad);
                foreach (var newGamepad in replacedWithGamepads)
                    WireGamepad(newGamepad);
                break;
            case NotifyCollectionChangedAction.Reset:
                using (var wiredGamepadsLockHeld = wiredGamepadsLock.Lock())
                {
                    foreach (var oldGamepad in wiredGamepads)
                        UnwireGamepad(oldGamepad, false);
                    wiredGamepads.Clear();
                }
                var gamepadsList = CreateGamepadsResetMessageGamepadList();
                var proxyResetMessage = new GamepadsResetMessage
                {
                    Gamepads = gamepadsList,
                    Type = nameof(HostMessageType.GamepadsReset).Underscore().ToLowerInvariant()
                };
                var bridgedUiResetMessage = new GamepadsResetMessage
                {
                    Gamepads = gamepadsList,
                    Type = nameof(HostMessageType.GamepadsReset).Camelize()
                };
                gamepadMessagingQueue.Enqueue(() => SendGamepadMessageAsync(proxyResetMessage, bridgedUiResetMessage));
                foreach (var newGamepad in gamepadInterop.Gamepads)
                    WireGamepad(newGamepad);
                break;
        }
    }

    void HandleGamepadThumbstickThumbstickUpdated(object? sender, ThumbstickUpdatedEventArgs e)
    {
        if (sender is not IObservableThumbstick observableThumbstick)
            return;
        var gamepad = observableThumbstick.Gamepad;
        var index = gamepadInterop.Gamepads.IndexOf(gamepad);
        var thumbstick = gamepad.Thumbsticks.IndexOf(observableThumbstick);
        var proxyMessage = new GamepadThumbstickChangedMessage
        {
            Direction = e.Direction,
            Index = index,
            Position = e.Position,
            Thumbstick = thumbstick,
            Type = nameof(HostMessageType.GamepadThumbstickChanged).Underscore().ToLowerInvariant(),
            X = e.X,
            Y = e.Y
        };
        var bridgedUiMessage = new GamepadThumbstickChangedMessage
        {
            Direction = e.Direction,
            Index = index,
            Position = e.Position,
            Thumbstick = thumbstick,
            Type = nameof(HostMessageType.GamepadThumbstickChanged).Camelize(),
            X = e.X,
            Y = e.Y
        };
        gamepadMessagingQueue.Enqueue(() => SendGamepadMessageAsync(proxyMessage, bridgedUiMessage));
    }

    void HandleGamepadTriggerTriggerUpdated(object? sender, TriggerUpdatedEventArgs e)
    {
        if (sender is not IObservableTrigger observableTrigger)
            return;
        var gamepad = observableTrigger.Gamepad;
        var index = gamepadInterop.Gamepads.IndexOf(gamepad);
        var trigger = gamepad.Triggers.IndexOf(observableTrigger);
        var proxyMessage = new GamepadTriggerChangedMessage
        {
            Index = index,
            Position = e.Position,
            Trigger = trigger,
            Type = nameof(HostMessageType.GamepadTriggerChanged).Underscore().ToLowerInvariant()
        };
        var bridgedUiMessage = new GamepadTriggerChangedMessage
        {
            Index = index,
            Position = e.Position,
            Trigger = trigger,
            Type = nameof(HostMessageType.GamepadTriggerChanged).Camelize()
        };
        gamepadMessagingQueue.Enqueue(() => SendGamepadMessageAsync(proxyMessage, bridgedUiMessage));
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.AllowModsToInterceptKeyStrokes))
        {
            if (!settings.AllowModsToInterceptKeyStrokes)
                ShutdownKeyInterception();
        }
        else if (e.PropertyName is nameof(ISettings.Type))
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
                    var saveGameData = Serializer.Deserialize<SaveGameData>(await savePackage.GetAsync(saveGameDataKey).ConfigureAwait(false));
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

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task<(int denialReason, ZipFile? archive)> InitializeUiLayerAsync(string? scriptMod, string uiRoot, Guid uniqueId, int layerIndex)
    {
        if (isBridgedUiDevelopmentModeEnabled
            && (Directory.Exists(uiRoot)
            || Uri.TryCreate(uiRoot, UriKind.Absolute, out _)))
            return (BridgedUiRequestResponseMessage.DenialReason_None, null);
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
            return (BridgedUiRequestResponseMessage.DenialReason_ScriptModNotFound, null);
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
                           mf.FoundAbsent == null
                        && mf.FileType == ModsDirectoryFileType.ScriptArchive
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
                ? await CacheScriptModAsync(scriptMod, uniqueId, layerIndex).ConfigureAwait(false)
                : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to cache script mod while preparing bridged UI {UniqueId}, Layer {LayerIndex} from {Path}", uniqueId, layerIndex, scriptMod);
        }
#pragma warning restore CA2000 // Dispose objects before losing scope
        if (archive is null)
            return (BridgedUiRequestResponseMessage.DenialReason_ScriptModNotFound, null);
        if (layerIndex is 0 && archive.GetEntry(Path.Combine([.. new string[] { uiRoot, "index.html" }.Where(segment => !string.IsNullOrWhiteSpace(segment))]).Replace("\\", "/", StringComparison.Ordinal)) is null)
        {
            ((IDisposable)archive).Dispose();
            return (BridgedUiRequestResponseMessage.DenialReason_IndexNotFound, null);
        }
        return (BridgedUiRequestResponseMessage.DenialReason_None, archive);
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
                await SendMessageToProxyAsync(new HostMessageBase
                {
                    Type = nameof(HostMessageType.SendLoadedSaveIdentifiers).Underscore().ToLowerInvariant()
                }).ConfigureAwait(false);
                using (var desktopInputInterceptorLockHeld = await desktopInputInterceptorLock.LockAsync().ConfigureAwait(false))
                    if (desktopInputInterceptorReferenceCounts.Count is > 0)
                        await SendMessageToProxyAsync(new KeyInterceptionResponseMessage
                        {
                            Type = nameof(HostMessageType.StartInterceptingKeysResponse).Underscore().ToLowerInvariant(),
                            KeyResults = desktopInputInterceptorReferenceCounts.Keys.ToImmutableDictionary(key => (int)key, key => 0)
                        }).ConfigureAwait(false);
                gamepadMessagingQueue.Enqueue(async () =>
                {
                    gamepadMessagingEnrolledClients.TryAdd(client, null);
                    await SendMessageToSpecificProxyAsync(client, new GamepadsResetMessage
                    {
                        Gamepads = CreateGamepadsResetMessageGamepadList(),
                        Type = nameof(HostMessageType.GamepadsReset).Underscore().ToLowerInvariant()
                    }).ConfigureAwait(false);
                });
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
        var layers = new List<(ZipFile? archive, string uiRoot)>();
        var layerIndex = requestMessage.Layers?.Count ?? 0;
        foreach (var requestedLayer in requestMessage.Layers ?? Enumerable.Empty<BridgedUiRequestMessageLayer>())
        {
            var (layerDenialReason, layerArchive) = await InitializeUiLayerAsync(requestedLayer.ScriptMod, requestedLayer.UiRoot, requestMessage.UniqueId, layerIndex--).ConfigureAwait(false);
            if (layerDenialReason != BridgedUiRequestResponseMessage.DenialReason_None)
            {
                await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, layerDenialReason).ConfigureAwait(false);
                return;
            }
        }
        var (denialReason, archive) = await InitializeUiLayerAsync(requestMessage.ScriptMod, requestMessage.UiRoot, requestMessage.UniqueId, 0).ConfigureAwait(false);
        if (denialReason != BridgedUiRequestResponseMessage.DenialReason_None)
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, denialReason).ConfigureAwait(false);
            return;
        }
        PromptPlayerForBridgedUiAuthorization(requestMessage, archive, layers.AsReadOnly());
    }

    async Task ProcessDesktopInputMessagesAsync()
    {
        var type = nameof(HostMessageType.KeyIntercepted).Underscore().ToLowerInvariant();
        while (await desktopInputMessagingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var (key, isDown) = await desktopInputMessagingQueue.DequeueAsync().ConfigureAwait(false);
            await SendMessageToProxyAsync(new KeyInterceptedMessage
            {
                Type = type,
                Key = (int)key,
                IsDown = isDown
            }).ConfigureAwait(false);
        }
    }

    async Task ProcessGamepadMessagesAsync()
    {
        while (await gamepadMessagingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var asyncFunc = await gamepadMessagingQueue.DequeueAsync().ConfigureAwait(false);
            try
            {
                await asyncFunc().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unhandled exception when processing gamepad messaging queue");
            }
        }
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
        foreach (var (locale, locKey, value) in await gameResourceCataloger.GetLocalizedStringsAsync(lookUpLocalizedStringsMessage.LocKeys, lookUpLocalizedStringsMessage.Locales).ConfigureAwait(false))
            response.Entries.Add(new()
            {
                Locale = locale,
                LocKey = locKey,
                Value = value
            });
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
                await SendMessageToProxyAsync(new BridgedUiAnnouncementMessage
                {
                    Type = nameof(HostMessageType.BridgedUiAnnouncement).Underscore().ToLowerInvariant(),
                    UniqueId = announcerUniqueId.ToString("n").ToLowerInvariant(),
                    Announcement = dynamicAnnouncement
                }).ConfigureAwait(false);
                SendMessageToBridgedUis(new BridgedUiAnnouncementMessage()
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
                gamepadMessagingQueue.Enqueue(() =>
                {
                    gamepadMessagingEnrolledBridgedUis.TryAdd(loaderUniqueId, null);
                    SendMessageToSpecificBridgedUi(loaderUniqueId, new GamepadsResetMessage
                    {
                        Gamepads = CreateGamepadsResetMessageGamepadList(),
                        Type = nameof(HostMessageType.GamepadsReset).Camelize()
                    });
                    return Task.CompletedTask;
                });
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
            case ComponentMessageType.GetScreenshotDetails when fromBridgedUiUniqueId is { } bridgedUiRequestingScreenshotMetadata:
                if (TryParseMessage<GetScreenshotDetailsMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "get screenshot details", out var getScreenshotMetadataMessage))
                {
                    var response = new GetScreenshotDetailsResponseMessage()
                    {
                        Type = nameof(HostMessageType.GetScreenshotDetailsResponse).Camelize(),
                        Name = getScreenshotMetadataMessage.Name
                    };
                    var screenshotFile = new FileInfo(Path.Combine(settings.UserDataFolderPath, "Screenshots", getScreenshotMetadataMessage.Name));
                    if (screenshotFile.Exists)
                    {
                        try
                        {
                            response.Attributes = screenshotFile.Attributes;
                            response.CreationTime = screenshotFile.CreationTime;
                            response.CreationTimeUtc = screenshotFile.CreationTimeUtc;
                            response.LastAccessTime = screenshotFile.LastAccessTime;
                            response.LastAccessTimeUtc = screenshotFile.LastAccessTimeUtc;
                            response.LastWriteTime = screenshotFile.LastWriteTime;
                            response.LastWriteTimeUtc = screenshotFile.LastWriteTimeUtc;
                            response.Size = screenshotFile.Length;
                            response.UnixFileMode = screenshotFile.UnixFileMode;
                            using var pngFileStream = screenshotFile.OpenRead();
                            var signatureArray = ArrayPool<byte>.Shared.Rent(8);
                            var uintArray = ArrayPool<byte>.Shared.Rent(4);
                            var ushortArray = ArrayPool<byte>.Shared.Rent(2);
                            try
                            {
                                Memory<byte> signatureMemory = signatureArray;
                                Memory<byte> uintMemory = uintArray;
                                Memory<byte> ushortMemory = ushortArray;
                                await pngFileStream.ReadExactlyAsync(signatureMemory[..8]).ConfigureAwait(false);
                                if (signatureMemory[..8].Span.SequenceEqual(pngSignature.Span))
                                    while (pngFileStream.Position < pngFileStream.Length)
                                    {
                                        await pngFileStream.ReadExactlyAsync(uintMemory[..4]).ConfigureAwait(false);
                                        var chunkLength = MemoryMarshal.Read<uint>(uintMemory.Span[..4]);
                                        chunkLength = BinaryPrimitives.ReverseEndianness(chunkLength);
                                        await pngFileStream.ReadExactlyAsync(uintMemory[..4]).ConfigureAwait(false);
                                        if (!uintMemory.Span[..4].SequenceEqual(screenshotPngCustomChunkType.Span))
                                        {
                                            pngFileStream.Seek(chunkLength + 4, SeekOrigin.Current);
                                            continue;
                                        }
                                        await pngFileStream.ReadExactlyAsync(uintMemory[..4]).ConfigureAwait(false);
                                        response.MetadataVersion = MemoryMarshal.Read<uint>(uintMemory.Span[..4]);
                                        await pngFileStream.ReadExactlyAsync(ushortMemory[..2]).ConfigureAwait(false);
                                        var keyValuePairs = MemoryMarshal.Read<ushort>(ushortMemory.Span[..2]);
                                        while (keyValuePairs-- > 0)
                                        {
                                            string key, value;
                                            await pngFileStream.ReadExactlyAsync(uintMemory[..4]).ConfigureAwait(false);
                                            var keyLength = (int)MemoryMarshal.Read<uint>(uintMemory.Span[..4]);
                                            var keyArray = ArrayPool<byte>.Shared.Rent(keyLength);
                                            try
                                            {
                                                Memory<byte> keyMemory = keyArray;
                                                await pngFileStream.ReadExactlyAsync(keyMemory[..keyLength]).ConfigureAwait(false);
                                                key = Encoding.UTF8.GetString(keyMemory.Span[..keyLength]);
                                            }
                                            finally
                                            {
                                                ArrayPool<byte>.Shared.Return(keyArray);
                                            }
                                            await pngFileStream.ReadExactlyAsync(uintMemory[..4]).ConfigureAwait(false);
                                            var valueLength = (int)MemoryMarshal.Read<uint>(uintMemory.Span[..4]);
                                            var valueArray = ArrayPool<byte>.Shared.Rent(valueLength);
                                            try
                                            {
                                                Memory<byte> valueMemory = valueArray;
                                                await pngFileStream.ReadExactlyAsync(valueMemory[..valueLength]).ConfigureAwait(false);
                                                value = Encoding.UTF8.GetString(valueMemory.Span[..valueLength]);
                                            }
                                            finally
                                            {
                                                ArrayPool<byte>.Shared.Return(valueArray);
                                            }
                                            response.Metadata.Add(key, value);
                                        }
                                        break;
                                    }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(signatureArray);
                                ArrayPool<byte>.Shared.Return(uintArray);
                                ArrayPool<byte>.Shared.Return(ushortArray);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "getting screenshot metadata for {ScreenshotName}", getScreenshotMetadataMessage.Name);
                        }
                    }
                    SendMessageToSpecificBridgedUi(bridgedUiRequestingScreenshotMetadata, response);
                }
                break;
            case ComponentMessageType.ListScreenshotNames when fromBridgedUiUniqueId is { } bridgedUiRequestingScreenshotsList:
                {
                    var listScreenshotsResponseMessage = new ListScreenshotNamesResponseMessage()
                    {
                        Type = nameof(HostMessageType.ListScreenshotNamesResponse).Camelize()
                    };
                    var screenshotsFolder = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "Screenshots"));
                    if (screenshotsFolder.Exists)
                        foreach (var pngFile in screenshotsFolder.GetFiles("*.png", SearchOption.TopDirectoryOnly))
                            listScreenshotsResponseMessage.Names.Add(pngFile.Name);
                    SendMessageToSpecificBridgedUi(bridgedUiRequestingScreenshotsList, listScreenshotsResponseMessage);
                }
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
            case ComponentMessageType.StartInterceptingKeys when fromBridgedUiUniqueId is null:
                {
                    if (TryParseMessage<KeyInterceptionMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "start intercepting keys", out var keyInterceptionMessage))
                    {
                        using var desktopInputInterceptorLockHeld = await desktopInputInterceptorLock.LockAsync().ConfigureAwait(false);
                        var results = new Dictionary<int, int>();
                        var keyInterceptionsStarted = new List<DesktopInputKey>();
                        foreach (var key in keyInterceptionMessage.Keys.Cast<DesktopInputKey>())
                        {
                            if (!Enum.IsDefined(typeof(DesktopInputKey), key)
                                || key is DesktopInputKey.None)
                            {
                                results.Add((int)key, 1);
                                continue;
                            }
                            if (!settings.AllowModsToInterceptKeyStrokes)
                            {
                                results.Add((int)key, 2);
                                continue;
                            }
                            if (desktopInputInterceptorReferenceCounts.Count >= 9
                                && !desktopInputInterceptorReferenceCounts.ContainsKey(key))
                            {
                                results.Add((int)key, 3);
                                continue;
                            }
                            if (desktopInputInterceptorReferenceCounts.TryGetValue(key, out var existingCount))
                            {
                                desktopInputInterceptorReferenceCounts[key] = existingCount + 1;
                                results.Add((int)key, 0);
                                continue;
                            }
                            await desktopInputInterceptor.StartMonitoringKeyAsync(key).ConfigureAwait(false);
                            desktopInputInterceptorReferenceCounts.Add(key, 1);
                            keyInterceptionsStarted.Add(key);
                            results.Add((int)key, 0);
                        }
                        await SendMessageToProxyAsync(new KeyInterceptionResponseMessage
                        {
                            Type = nameof(HostMessageType.StartInterceptingKeysResponse).Underscore().ToLowerInvariant(),
                            RequestId = keyInterceptionMessage.RequestId,
                            KeyResults = results
                        }).ConfigureAwait(false);
                        if (settings.NotifyOnModKeyStrokeInterceptionChanges && keyInterceptionsStarted.Count is > 0)
                            await ShowNotificationAsync(string.Format(AppText.ProxyHost_IGN_StartInterceptingKeys_Text, keyInterceptionsStarted.OrderBy(key => key.ToString()).Humanize(AppText.Conjunction_Or), keyInterceptionsStarted.Count is > 1 ? AppText.ProxyHost_IGN_KeyInterception_Key.Pluralize() : AppText.ProxyHost_IGN_KeyInterception_Key), AppText.ProxyHost_IGN_StartInterceptingKeys_Title, Fnv64.SetHighBit(Fnv64.GetHash("PlumbBuddy.Integration.DesktopInputInterceptorIcon.png")));
                    }
                }
                break;
            case ComponentMessageType.StopInterceptingKeys when fromBridgedUiUniqueId is null:
                {
                    if (TryParseMessage<KeyInterceptionMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "stop intercepting keys", out var keyInterceptionMessage))
                    {
                        using var desktopInputInterceptorLockHeld = await desktopInputInterceptorLock.LockAsync().ConfigureAwait(false);
                        var results = new Dictionary<int, int>();
                        var keyInterceptionsStopped = new List<DesktopInputKey>();
                        foreach (var key in keyInterceptionMessage.Keys.Cast<DesktopInputKey>())
                        {
                            if (!Enum.IsDefined(typeof(DesktopInputKey), key)
                                || key is DesktopInputKey.None)
                            {
                                results.Add((int)key, 2);
                                continue;
                            }
                            if (!desktopInputInterceptorReferenceCounts.TryGetValue(key, out var existingCount))
                            {
                                results.Add((int)key, 3);
                                continue;
                            }
                            if (existingCount is > 1)
                            {
                                desktopInputInterceptorReferenceCounts[key] = existingCount - 1;
                                results.Add((int)key, 1);
                                continue;
                            }
                            await desktopInputInterceptor.StopMonitoringKeyAsync(key).ConfigureAwait(false);
                            desktopInputInterceptorReferenceCounts.Remove(key);
                            keyInterceptionsStopped.Add(key);
                            results.Add((int)key, 0);
                        }
                        await SendMessageToProxyAsync(new KeyInterceptionResponseMessage
                        {
                            Type = nameof(HostMessageType.StopInterceptingKeysResponse).Underscore().ToLowerInvariant(),
                            RequestId = keyInterceptionMessage.RequestId,
                            KeyResults = results
                        }).ConfigureAwait(false);
                        if (settings.NotifyOnModKeyStrokeInterceptionChanges && keyInterceptionsStopped.Count is > 0)
                            await ShowNotificationAsync(string.Format(AppText.ProxyHost_IGN_StopInterceptingKeys_Text, keyInterceptionsStopped.OrderBy(key => key.ToString()).Humanize(AppText.Conjunction_Or), keyInterceptionsStopped.Count is > 1 ? AppText.ProxyHost_IGN_KeyInterception_Key.Pluralize() : AppText.ProxyHost_IGN_KeyInterception_Key), AppText.ProxyHost_IGN_StopInterceptingKeys_Title, Fnv64.SetHighBit(Fnv64.GetHash("PlumbBuddy.Integration.DesktopInputInterceptorIcon.png")));
                    }
                }
                break;
            case ComponentMessageType.VibrateGamepad:
                if (TryParseMessage<VibrateGamepadMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "vibrate gamepad", out var vibrateGamepadMessage)
                    && vibrateGamepadMessage.Intensity >= 0
                    && vibrateGamepadMessage.Intensity <= 1
                    && gamepadInterop.Gamepads.ElementAtOrDefault(vibrateGamepadMessage.Index) is { } observableGamepad)
                    await observableGamepad.VibrateAsync(vibrateGamepadMessage.Intensity, TimeSpan.FromSeconds(vibrateGamepadMessage.Duration)).ConfigureAwait(false);
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
                var saveGameData = Serializer.Deserialize<SaveGameData>(await savePackage.GetAsync(saveGameDataKey, cancellationToken: cancellationToken).ConfigureAwait(false));
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
                var saveGameData = Serializer.Deserialize<SaveGameData>(await savePackage.GetAsync(saveGameDataKey, cancellationToken: cancellationToken).ConfigureAwait(false));
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

    void PromptPlayerForBridgedUiAuthorization(BridgedUiRequestMessage request, ZipFile? archive, IReadOnlyList<(ZipFile? archive, string uiRoot)> layers)
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
            loadedBridgedUis.AddOrUpdate(request.UniqueId, layers.Select(t => t.archive).Where(archive => archive is not null).Cast<ZipFile>().Concat(archive is null ? [] : [archive]).ToList().AsReadOnly(), (k, v) => v);
            await BridgedUiRequestResolvedAsync(request.UniqueId, BridgedUiRequestResponseMessage.DenialReason_None).ConfigureAwait(false);
            BridgedUiAuthorized?.Invoke(this, new()
            {
                Archive = archive,
                HostName = request.HostName,
                Layers = layers,
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

    async Task SendGamepadMessageAsync(HostMessageBase proxyMessage, HostMessageBase bridgedUiMessage)
    {
        foreach (var client in gamepadMessagingEnrolledClients.Keys)
            await SendMessageToSpecificProxyAsync(client, proxyMessage).ConfigureAwait(false);
        foreach (var uniqueId in gamepadMessagingEnrolledBridgedUis.Keys)
            SendMessageToSpecificBridgedUi(uniqueId, bridgedUiMessage);
    }

    void SendMessageToBridgedUis(HostMessageBase message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        BridgedUiMessageSent?.Invoke(this, new()
        {
            MessageJson = JsonSerializer.Serialize<object>(message, bridgedUiJsonSerializerOptions)
        });
    }

    void SendMessageToSpecificBridgedUi(Guid uniqueId, HostMessageBase message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        SpecificBridgedUiMessageSent?.Invoke(this, new()
        {
            MessageJson = JsonSerializer.Serialize<object>(message, bridgedUiJsonSerializerOptions),
            UniqueId = uniqueId
        });
    }

    async Task SendMessageToProxyAsync(HostMessageBase message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        if (connectedClients.IsEmpty)
            return;
        var serializedMessageBytes = JsonSerializer.SerializeToUtf8Bytes<object>(message, proxyJsonSerializerOptions);
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

    async Task SendMessageToSpecificProxyAsync(TcpClient client, HostMessageBase message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        if (!connectedClients.TryGetValue(client, out var clientWriteLock))
            return;
        var serializedMessageBytes = JsonSerializer.SerializeToUtf8Bytes<object>(message, proxyJsonSerializerOptions);
        Memory<byte> serializedMessageSizeBytes = new byte[4];
        var serializedMessageSize = BinaryPrimitives.ReverseEndianness(serializedMessageBytes.Length);
        MemoryMarshal.Write(serializedMessageSizeBytes.Span, in serializedMessageSize);
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

    public Task ShowNotificationAsync(string text, string? title = null, ulong? iconInstance = null) =>
        SendMessageToProxyAsync(new ShowNotificationMessage
        {
            Type = nameof(HostMessageType.ShowNotification).Underscore().ToLowerInvariant(),
            Text = text,
            Title = title,
            IconInstance = iconInstance?.ToString("x16")
        });

    void ShutdownKeyInterception() =>
        _ = Task.Run(ShutdownKeyInterceptionAsync);

    async Task ShutdownKeyInterceptionAsync()
    {
        using var desktopInputInterceptorLockHeld = await desktopInputInterceptorLock.LockAsync().ConfigureAwait(false);
        if (desktopInputInterceptorReferenceCounts.Count is <= 0)
            return;
        var results = new Dictionary<int, int>();
        var keyInterceptionsStopped = new List<DesktopInputKey>();
        foreach (var key in desktopInputInterceptorReferenceCounts.Keys.ToImmutableArray())
        {
            await desktopInputInterceptor.StopMonitoringKeyAsync(key).ConfigureAwait(false);
            desktopInputInterceptorReferenceCounts.Remove(key);
            results.Add((int)key, 0);
            keyInterceptionsStopped.Add(key);
        }
        await SendMessageToProxyAsync(new KeyInterceptionResponseMessage
        {
            Type = nameof(HostMessageType.StopInterceptingKeysResponse).Underscore().ToLowerInvariant(),
            KeyResults = results
        }).ConfigureAwait(false);
        if (settings.NotifyOnModKeyStrokeInterceptionChanges && keyInterceptionsStopped.Count is > 0)
            await ShowNotificationAsync($"Your mods are no longer listening for when you press the {keyInterceptionsStopped.Humanize("or")} {(keyInterceptionsStopped.Count is > 1 ? "key".Pluralize() : "key")}.", "PlumbBuddy", Fnv64.SetHighBit(Fnv64.GetHash("PlumbBuddy.Integration.DesktopInputInterceptorIcon.png")));
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

    void UnwireGamepad(IObservableGamepad oldGamepad, bool acquireLock = true)
    {
        if (acquireLock)
        {
            using var wiredGamepadsLockHeld = wiredGamepadsLock.Lock();
            if (!wiredGamepads.Remove(oldGamepad))
                return;
        }
        foreach (var gamepadButton in oldGamepad.Buttons)
            gamepadButton.ButtonUpdated -= HandleGamepadButtonButtonUpdated;
        foreach (var gamepadThumbstick in oldGamepad.Thumbsticks)
            gamepadThumbstick.ThumbstickUpdated -= HandleGamepadThumbstickThumbstickUpdated;
        foreach (var gamepadTrigger in oldGamepad.Triggers)
            gamepadTrigger.TriggerUpdated -= HandleGamepadTriggerTriggerUpdated;
    }

    public Task WaitForSavesAccessAsync(CancellationToken cancellationToken = default) =>
        reserveSavesAccessManualResetEvent.WaitAsync(cancellationToken);

    void WireGamepad(IObservableGamepad newGamepad)
    {
        using (var wiredGamepadsLockHeld = wiredGamepadsLock.Lock())
        {
            if (wiredGamepads.Contains(newGamepad))
                return;
            wiredGamepads.Add(newGamepad);
        }
        foreach (var gamepadButton in newGamepad.Buttons)
            gamepadButton.ButtonUpdated += HandleGamepadButtonButtonUpdated;
        foreach (var gamepadThumbstick in newGamepad.Thumbsticks)
            gamepadThumbstick.ThumbstickUpdated += HandleGamepadThumbstickThumbstickUpdated;
        foreach (var gamepadTrigger in newGamepad.Triggers)
            gamepadTrigger.TriggerUpdated += HandleGamepadTriggerTriggerUpdated;
    }

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
}
