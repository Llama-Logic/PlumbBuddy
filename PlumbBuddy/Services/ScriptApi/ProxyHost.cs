using Serializer = ProtoBuf.Serializer;

namespace PlumbBuddy.Services;

[SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "This is for Python, CA. You have your head up your ass.")]
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

    [GeneratedRegex(@"^N\-(?<nucleusId>[\da-f]{16})\-C\-(?<created>[\da-f]{16})\-W\-(?<simNow>[\da-f]{16})$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetSaveSpecificDataStorageFileSystemContainerPattern();

    [GeneratedRegex(@"[\da-f]{64}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetSha256HashHexPattern();

    [GeneratedRegex(@"^[a-z\d\-]+$")]
    private static partial Regex GetValidDnsHostnamePattern();

    static bool TryParseMessage<T>(JsonElement messageRoot, string messageJson, JsonSerializerOptions jsonSerializerOptions, ILogger<ProxyHost> logger, string messageDescription, [NotNullWhen(true)] out T? message)
        where T : class
    {
        try
        {
            if (messageRoot.Deserialize<T>(jsonSerializerOptions) is not { } nullableBridgedUiRequestMessage)
            {
                logger.LogWarning("failed to parse {MessageDescription} (value was null): {Message}", messageDescription, messageJson);
                message = null;
                return false;
            }
            message = nullableBridgedUiRequestMessage;
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
        gameIsLoadingManualResetEvent = new(true);
        gameIsSavingManualResetEvent = new(true);
        saveSpecificDataStorageConnectionLocks = new();
        saveSpecificDataStorageInitializationLock = new();
        saveSpecificDataStoragePropagationLock = new();
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
    ulong lastSaveSimNow;
    readonly TcpListener listener;
    readonly ConcurrentDictionary<Guid, ZipFile?> loadedBridgedUis;
    readonly ILogger<ProxyHost> logger;
    AsyncProducerConsumerQueue<string>? pathsProcessingQueue;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly AsyncManualResetEvent gameIsLoadingManualResetEvent;
    readonly AsyncManualResetEvent gameIsSavingManualResetEvent;
    readonly IPlatformFunctions platformFunctions;
    readonly ConcurrentDictionary<Guid, AsyncLock> saveSpecificDataStorageConnectionLocks;
    ulong saveCreated;
    ulong saveNucleusId;
    ulong saveSimNow;
    ulong saveSlotId;
    readonly AsyncLock saveSpecificDataStorageInitializationLock;
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
        File.Copy(path, cachePath, true);
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
        fileSystemWatcher = new FileSystemWatcher(userDataFolder.FullName)
        {
            IncludeSubdirectories = true,
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
        var stream = client.GetStream();
        try
        {
            Memory<byte> serializedMessageSizeBuffer = new byte[4];
            while (client.Connected)
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
                foreach (var loadedBridgedUiUniqueId in loadedBridgedUis.Keys)
                    await DestroyBridgedUiAsync(loadedBridgedUiUniqueId).ConfigureAwait(false);
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

    async Task InitializeSaveSpecificRelationalDataStorageAsync()
    {
        using var saveSpecificDataStorageInitializationLockHeld = await saveSpecificDataStorageInitializationLock.LockAsync().ConfigureAwait(false);
        await gameIsLoadingManualResetEvent.WaitAsync().ConfigureAwait(false);
        var appDataDirectory = await GetAppDataDirectoryAsync().ConfigureAwait(false);
        var rdsDirectory = Directory.CreateDirectory(Path.Combine(appDataDirectory.FullName, "Relational Data Storage"));
        var candidateDirectories = rdsDirectory.GetDirectories()
            .Select(d =>
            {
                var match = GetSaveSpecificDataStorageFileSystemContainerPattern().Match(d.Name);
                if (match.Success
                    && ulong.TryParse(match.Groups["nucleusId"].Value, NumberStyles.HexNumber, null, out var nucleusId)
                    && ulong.TryParse(match.Groups["created"].Value, NumberStyles.HexNumber, null, out var created)
                    && ulong.TryParse(match.Groups["simNow"].Value, NumberStyles.HexNumber, null, out var simNow))
                    return (nucleusId, created, simNow);
                return (nucleusId: 0UL, created: 0UL, simNow: 0UL);
            })
            .Where(t => t.nucleusId == saveNucleusId && t.created == saveCreated && t.simNow <= saveSimNow);
        if (candidateDirectories.Any())
        {
            (_, _, saveSimNow) = candidateDirectories.OrderByDescending(t => t.simNow).FirstOrDefault();
            return;
        }
        var content = ReadOnlyMemory<byte>.Empty;
        if (saveSpecificDataStorageSnapshot is { } memoryResidentSnapshot
            && saveSpecificDataStorageSnapshotNucleusId == saveNucleusId
            && saveSpecificDataStorageSnapshotCreated == saveCreated
            && saveSpecificDataStorageSnapshotSlotId == saveSlotId
            && saveSpecificDataStorageSnapshotSimNow <= saveSimNow)
            (_, content) = await memoryResidentSnapshot.ConfigureAwait(false);
        if (content.IsEmpty)
        {
            var savesDirectory = new DirectoryInfo(Path.Combine(settings.UserDataFolderPath, "saves"));
            if (!savesDirectory.Exists)
                return;
            var candidates = new List<(ulong simNow, DataBasePackedFile savePackage)>();
            var saveFileNameForSlot = $"Slot_{(uint)saveSlotId:x8}.save";
            static async Task considerCandidateAsync(List<(ulong simNow, DataBasePackedFile savePackage)> candidates, ulong saveNucleusId, ulong saveCreated, ulong saveSlotId, ulong saveSimNow, FileInfo saveFile)
            {
                var savePackage = await DataBasePackedFile.FromPathAsync(saveFile.FullName).ConfigureAwait(false);
                var packageKeys = await savePackage.GetKeysAsync().ConfigureAwait(false);
                var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                if (saveGameDataKeys.Length is <= 0 or >= 2)
                {
                    await savePackage.DisposeAsync().ConfigureAwait(false);
                    return;
                }
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
                {
                    await savePackage.DisposeAsync().ConfigureAwait(false);
                    return;
                }
                candidates.Add((gamePlayData.WorldGameTime, savePackage));
            }
            foreach (var saveFile in savesDirectory.GetFiles().Where(file => file.Name.Equals(saveFileNameForSlot, StringComparison.OrdinalIgnoreCase)))
                await considerCandidateAsync(candidates, saveNucleusId, saveCreated, saveSlotId, saveSimNow, saveFile).ConfigureAwait(false);
            if (candidates.Count is 0)
            {
                foreach (var saveFile in savesDirectory.GetFiles().Where(file => !file.Name.Equals(saveFileNameForSlot, StringComparison.OrdinalIgnoreCase)))
                    await considerCandidateAsync(candidates, saveNucleusId, saveCreated, saveSlotId, saveSimNow, saveFile).ConfigureAwait(false);
            }
            if (candidates.Count is 0)
                return;
            var orderedCandidates = candidates
                .OrderByDescending(t => t.simNow);
            var (simNow, savePackage) = orderedCandidates.First();
            saveSimNow = simNow;
            foreach (var (_, losingCandidate) in orderedCandidates.Skip(1))
                await losingCandidate.DisposeAsync().ConfigureAwait(false);
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
            finally
            {
                await savePackage.DisposeAsync().ConfigureAwait(false);
            }
        }
        try
        {
            var saveSpecificDirectory = new DirectoryInfo(Path.Combine(rdsDirectory.FullName, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}-W-{saveSimNow:x16}"));
            foreach (var alternateSaveSpecificDirectory in rdsDirectory.GetDirectories().Where(d => d.Name != saveSpecificDirectory.Name))
                alternateSaveSpecificDirectory.Delete(true);
            if (saveSpecificDirectory.Exists)
                return;
            saveSpecificDirectory.Create();
            using var contentStream = new ReadOnlyMemoryOfByteStream(content);
            using var zipInputStream = new ZipInputStream(contentStream);
            ZipEntry entry;
            while ((entry = zipInputStream.GetNextEntry()) is not null)
            {
                if (entry.IsDirectory)
                    continue;
                using var fileStream = File.Create(Path.Combine(saveSpecificDirectory.FullName, entry.Name));
                await zipInputStream.CopyToAsync(fileStream).ConfigureAwait(false);
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
                        gameIsLoadingManualResetEvent.Reset();
                    else if (gameServiceEvent is GameServiceEvent.OnAllHouseholdsAndSimInfosLoaded)
                        gameIsLoadingManualResetEvent.Set();
                    else if (gameServiceEvent is GameServiceEvent.PreSave)
                        gameIsSavingManualResetEvent.Reset();
                    else if (gameServiceEvent is GameServiceEvent.Save)
                        gameIsSavingManualResetEvent.Set();
                    if (gameServiceEvent is GameServiceEvent.Load or GameServiceEvent.PreSave
                        && gameServiceEventMessage.NucleusId is { } gameServiceEventNucleusId
                        && gameServiceEventMessage.Created is { } gameServiceEventCreated
                        && gameServiceEventMessage.SimNow is { } gameServiceEventSimNow
                        && gameServiceEventMessage.SlotId is { } gameServiceEventSlotId)
                    {
                        if (gameServiceEvent is GameServiceEvent.PreSave
                            && gameServiceEventSlotId is 0)
                            break; // don't care about scratch saves
                        if (gameServiceEvent is GameServiceEvent.PreSave
                            && saveNucleusId == gameServiceEventNucleusId
                            && saveCreated == gameServiceEventCreated)
                            lastSaveSimNow = saveSimNow;
                        saveNucleusId = gameServiceEventNucleusId;
                        saveCreated = gameServiceEventCreated;
                        saveSimNow = gameServiceEventSimNow;
                        saveSlotId = gameServiceEventSlotId;
                        if (gameServiceEvent is GameServiceEvent.PreSave
                            && saveSpecificDataStorageSnapshot is null)
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
        while (await pathsProcessingQueue.OutputAvailableAsync().ConfigureAwait(false))
        {
            var alreadyNomed = new OrderedHashSet<string>
            {
                await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false)
            };
            while (true)
            {
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
                        alreadyNomed.Add(await pathsProcessingQueue.DequeueAsync().ConfigureAwait(false));
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
            if ((await platformFunctions.GetGameProcessAsync(new DirectoryInfo(settings.InstallationFolderPath)).ConfigureAwait(false))?.StartTime is not { } gameStarted)
                continue;
            var nomNom = new Queue<string>(alreadyNomed);
            try
            {
                var savesDirectoryPath = Path.GetFullPath(Path.Combine(settings.UserDataFolderPath, "saves"));
                while (nomNom.TryDequeue(out var path))
                {
                    FileSystemInfo? fileSystemInfo = File.Exists(path)
                        ? new FileInfo(path)
                        : Directory.Exists(path)
                        ? new DirectoryInfo(path)
                        : null;
                    if (fileSystemInfo is null)
                        continue;
                    var isInSavesDirectory = savesDirectoryPath == Path.GetFullPath(fileSystemInfo is FileInfo fileInfo
                        ? fileInfo.Directory!.FullName
                        : fileSystemInfo is DirectoryInfo directoryInfo
                        ? directoryInfo.FullName
                        : string.Empty);
                    if (!isInSavesDirectory)
                        continue;
                    var savesDirectoryInfo = new DirectoryInfo(savesDirectoryPath);
                    var singleSaveFile = new FileInfo(path);
                    if (!singleSaveFile.Exists
                        || !singleSaveFile.Extension.Equals(".save", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var length = singleSaveFile.Length;
                    while (singleSaveFile.LastWriteTime > gameStarted)
                    {
                        await Task.Delay(oneQuarterSecond).ConfigureAwait(false);
                        singleSaveFile.Refresh();
                        var newLength = singleSaveFile.Length;
                        if (length == newLength)
                            break;
                        length = newLength;
                    }
                    using var savePackage = await DataBasePackedFile.FromPathAsync(singleSaveFile.FullName, forReadOnly: false).ConfigureAwait(false);
                    var packageKeys = await savePackage.GetKeysAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    var saveGameDataKeys = packageKeys.Where(k => k.Type is ResourceType.SaveGameData).ToImmutableArray();
                    if (saveGameDataKeys.Length is <= 0 or >= 2)
                        continue;
                    var saveGameDataKey = saveGameDataKeys[0];
                    var saveGameData = Serializer.Deserialize<ArchivistSaveGameData>(await savePackage.GetAsync(saveGameDataKey, cancellationToken: cancellationToken).ConfigureAwait(false));
                    if (saveGameData is null)
                        continue;
                    if (saveGameData.Account is not { } account
                        || account.NucleusId != saveNucleusId
                        || account.Created != saveCreated
                        || saveGameData.SaveSlot is not { } saveSlot
                        || saveSlot.SlotId != saveSlotId
                        || saveSlot.GameplayData is not { } gamePlayData
                        || gamePlayData.WorldGameTime < saveSimNow)
                        continue;
                    if (saveSpecificDataStorageSnapshot is null)
                        break;
                    DisconnectFromSavesFolder();
                    var (saveSpecificDataKey, saveSpecificDataContent) = await saveSpecificDataStorageSnapshot.ConfigureAwait(false);
                    saveSpecificDataStorageSnapshot = null;
                    if (saveSpecificDataContent.IsEmpty)
                        break;
                    foreach (var key in packageKeys.Where(pk => pk.Type is ResourceType.SaveSpecificRelationalDataStorage))
                        savePackage.Delete(key);
                    await savePackage.SetAsync(saveSpecificDataKey, saveSpecificDataContent, CompressionMode.ForceZLib).ConfigureAwait(false);
                    await savePackage.SaveAsync().ConfigureAwait(false);
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "encountered unhandled exception while processing the paths queue");
            }
        }
    }

    [SuppressMessage("Security", "CA2100: Review SQL queries for security vulnerabilities", Justification = "That's on modders. We give them parameters.")]
    async Task ProcessQueryRelationalDataStorageMessageAsync(QueryRelationalDataStorageMessage queryRelationalDataStorageMessage)
    {
        var errorCode = 0;
        string? errorMessage = null;
        var extendedErrorCode = 0;
        var recordSets = new List<RelationalDataStorageQueryRecordSet>();
        var executionStopwatch = new Stopwatch();
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
                                value = new { base64 = Convert.ToBase64String(bytes is byte[] bytesArray ? bytesArray : [..bytes]) };
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
            catch (Exception ex)
            {
                errorCode = -1;
                extendedErrorCode = -1;
                errorMessage = ex.Message;
            }
            finally
            {
                executionStopwatch.Stop();
            }
        }).ConfigureAwait(false);
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
        var appDataDirectory = await GetAppDataDirectoryAsync().ConfigureAwait(false);
        var rdsPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
        Directory.CreateDirectory(rdsPath);
        var saveSpecificDirectory = new DirectoryInfo(Path.Combine(rdsPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}-W-{lastSaveSimNow:x16}"));
        if (!saveSpecificDirectory.Exists)
            return (default, ReadOnlyMemory<byte>.Empty);
        var saveSpecificFiles = saveSpecificDirectory.GetFiles("*.sqlite", SearchOption.TopDirectoryOnly);
        if (saveSpecificFiles.Length is 0)
        {
            saveSpecificDirectory.Delete(true);
            return (default, ReadOnlyMemory<byte>.Empty);
        }
        using var zipFileStream = new ArrayBufferWriterOfByteStream();
        using var zipOutputStream = new ZipOutputStream(zipFileStream) { IsStreamOwner = false };
        zipOutputStream.SetLevel(0);
        foreach (var file in saveSpecificFiles)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = file.FullName,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            };
            using (var connection = new SqliteConnection(connectionStringBuilder.ConnectionString))
            {
                await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
                var com = connection.CreateCommand();
                com.CommandText = $"VACUUM;";
                await com.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                await connection.CloseAsync().ConfigureAwait(false);
            }
            file.Refresh();
            await zipOutputStream.PutNextEntryAsync(new ZipEntry(file.Name)
            {
                DateTime = file.LastWriteTime,
                Size = file.Length
            }, CancellationToken.None).ConfigureAwait(false);
            using var fileStream = file.OpenRead();
            await fileStream.CopyToAsync(zipOutputStream, CancellationToken.None).ConfigureAwait(false);
            await zipOutputStream.CloseEntryAsync(CancellationToken.None).ConfigureAwait(false);
        }
        await zipOutputStream.FinishAsync(CancellationToken.None).ConfigureAwait(false);
        saveSpecificDirectory.Delete(true);
        return (new(ResourceType.SaveSpecificRelationalDataStorage, 0x80000000, 0), zipFileStream.WrittenMemory);
    }

    public Task WaitForGameToFinishSavingAsync(CancellationToken cancellationToken = default) =>
        gameIsSavingManualResetEvent.WaitAsync(cancellationToken);

    async Task WithRelationalDataStorageConnectionAsync(Guid uniqueId, bool isSaveSpecific, Func<SqliteConnection, Task> withSqliteConnectionAsyncAction, CancellationToken cancellationToken = default)
    {
        IDisposable? saveSpecificDataStoragePropagationLockHeld = null, dataStorageConnectionLockHeld = null;
        if (isSaveSpecific)
        {
            saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
            await InitializeSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false);
        }
        try
        {
            var appDataDirectory = await GetAppDataDirectoryAsync().ConfigureAwait(false);
            var databaseDirectoryPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
            if (isSaveSpecific)
            {
                var saveSpecificDirectory = new DirectoryInfo(Path.Combine(databaseDirectoryPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}-W-{saveSimNow:x16}"));
                if (!saveSpecificDirectory.Exists)
                    saveSpecificDirectory.Create();
                databaseDirectoryPath = saveSpecificDirectory.FullName;
                dataStorageConnectionLockHeld = await saveSpecificDataStorageConnectionLocks.GetOrAdd(uniqueId, _ => new()).LockAsync(CancellationToken.None).ConfigureAwait(false);
            }
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
        finally
        {
            dataStorageConnectionLockHeld?.Dispose();
            saveSpecificDataStoragePropagationLockHeld?.Dispose();
        }
    }
}
