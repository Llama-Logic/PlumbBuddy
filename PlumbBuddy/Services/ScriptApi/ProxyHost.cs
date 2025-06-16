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

    [GeneratedRegex(@"[\da-f]{64}", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetSha256HashHexPattern();

    static bool TryParseMessage<T>(JsonElement messageRoot, string messageJson, JsonSerializerOptions jsonSerializerOptions, ILogger<ProxyHost> logger, string messageDescription, [NotNullWhen(true)] out T? message)
        where T : class
    {
        try
        {
            if (messageRoot.Deserialize<T>(jsonSerializerOptions) is not { } nullableBridgedUiRequestMessage)
            {
                logger.LogWarning($"failed to parse {messageDescription} (value was null): {{Message}}", messageJson);
                message = null;
                return false;
            }
            message = nullableBridgedUiRequestMessage;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"failed to parse {messageDescription}: {{Message}}", messageJson);
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
        relationalDataStorageConnectionLocks = new();
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
    bool isBridgedUiDevelopmentModeEnabled;
    bool isClientConnected;
    readonly TcpListener listener;
    readonly ConcurrentDictionary<Guid, ICSharpCode.SharpZipLib.Zip.ZipFile?> loadedBridgedUis;
    readonly ILogger<ProxyHost> logger;
    readonly IDbContextFactory<PbDbContext> pbDbContextFactory;
    readonly IPlatformFunctions platformFunctions;
    readonly ConcurrentDictionary<(Guid uniqueId, bool isSaveSpecific), AsyncLock> relationalDataStorageConnectionLocks;
    ulong saveCreated;
    ulong saveNucleusId;
    ulong saveSimNow;
    readonly AsyncReaderWriterLock saveSpecificDataStoragePropagationLock;
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

    async Task<ICSharpCode.SharpZipLib.Zip.ZipFile?> CacheScriptModAsync(string modsDirectoryRelativePath, Guid uniqueId)
    {
        var path = Path.Combine(settings.UserDataFolderPath, "Mods", modsDirectoryRelativePath);
        if (!File.Exists(path))
            return null;
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.CacheDirectory));
        var cacheDirectory = await tcs.Task.ConfigureAwait(false);
        var cachePath = Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs");
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);
        cachePath = Path.Combine(cachePath, $"{uniqueId:n}.ts4script");
        File.Copy(path, cachePath, true);
        try
        {
            return new ICSharpCode.SharpZipLib.Zip.ZipFile(cachePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "failed to load script mod at {Path}", path);
            return null;
        }
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
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.CacheDirectory));
        var cacheDirectory = await tcs.Task.ConfigureAwait(false);
        var cachedArchive = new FileInfo(Path.Combine(cacheDirectory.FullName, "UI Bridge Tabs", $"{uniqueId:n}.ts4script"));
        if (cachedArchive.Exists)
            cachedArchive.Delete();
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

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Type))
            IsBridgedUiDevelopmentModeEnabled = false;
    }

    async Task InitializeSaveSpecificRelationalDataStorageAsync(CancellationToken cancellationToken = default)
    {
        using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.WriterLockAsync(cancellationToken).ConfigureAwait(false);
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.AppDataDirectory));
        var appDataDirectory = await tcs.Task.ConfigureAwait(false);
        var rdsPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
        Directory.CreateDirectory(rdsPath);
        var saveSpecificDirectory = new DirectoryInfo(Path.Combine(rdsPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}"));
        if (!saveSpecificDirectory.Exists)
            saveSpecificDirectory.Create();
        var restoreDirectory = saveSpecificDirectory
            .GetDirectories("*.*", SearchOption.TopDirectoryOnly)
            .Select(directory => (directory, simNow: ulong.TryParse(directory.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var simNow) ? simNow : ulong.MaxValue))
            .Where(t => t.simNow <= saveSimNow)
            .OrderByDescending(t => t.simNow)
            .Take(1)
            .Select(t => t.directory)
            .FirstOrDefault();
        foreach (var file in saveSpecificDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            file.Delete();
        if (restoreDirectory is not null)
            foreach (var file in restoreDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                file.CopyTo(Path.Combine(saveSpecificDirectory.FullName, file.Name));
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
        ICSharpCode.SharpZipLib.Zip.ZipFile? archive = null;
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
        if (archive.GetEntry(Path.Combine([.. new string[] { requestMessage.UiRoot, "index.html" }.Where(segment => !string.IsNullOrWhiteSpace(segment))]).Replace("\\", "/", StringComparison.Ordinal)) is null)
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
                        saveCreated = gameServiceEventMessage.Created;
                        saveNucleusId = gameServiceEventMessage.NucleusId;
                        saveSimNow = gameServiceEventMessage.SimNow;
                        await InitializeSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false);
                    }
                    else if (gameServiceEvent is GameServiceEvent.PreSave)
                    {
                        saveCreated = gameServiceEventMessage.Created;
                        saveNucleusId = gameServiceEventMessage.NucleusId;
                        saveSimNow = gameServiceEventMessage.SimNow;
                        await SnapshotSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false);
                    }
                }
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
                if (TryParseMessage<SendLoadedSaveIdentifiersResponseMessage>(messageRoot, messageJson, jsonSerializerOptions, logger, "send loaded save identifiers response", out var sendLoadedSaveIdentifiersResponseMessage))
                {
                    saveCreated = sendLoadedSaveIdentifiersResponseMessage.Created;
                    saveNucleusId = sendLoadedSaveIdentifiersResponseMessage.NucleusId;
                    saveSimNow = sendLoadedSaveIdentifiersResponseMessage.SimNow;
                    await InitializeSaveSpecificRelationalDataStorageAsync().ConfigureAwait(false);
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

    [SuppressMessage("Security", "CA2100: Review SQL queries for security vulnerabilities", Justification = "That's on modders. We give them parameters.")]
    async Task ProcessQueryRelationalDataStorageMessageAsync(QueryRelationalDataStorageMessage queryRelationalDataStorageMessage)
    {
        var errorCode = 0;
        string? errorMessage = null;
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
                errorCode = sqlEx.ErrorCode;
                errorMessage = sqlEx.Message;
            }
            catch (Exception ex)
            {
                errorCode = -1;
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

    void PromptPlayerForBridgedUiAuthorization(BridgedUiRequestMessage request, ICSharpCode.SharpZipLib.Zip.ZipFile? archive = null)
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
    async Task SnapshotSaveSpecificRelationalDataStorageAsync(CancellationToken cancellationToken = default)
    {
        using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.WriterLockAsync(cancellationToken).ConfigureAwait(false);
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.AppDataDirectory));
        var appDataDirectory = await tcs.Task.ConfigureAwait(false);
        var rdsPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
        Directory.CreateDirectory(rdsPath);
        var saveSpecificDirectory = new DirectoryInfo(Path.Combine(rdsPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}"));
        if (!saveSpecificDirectory.Exists)
            saveSpecificDirectory.Create();
        var saveSpecificFiles = saveSpecificDirectory.GetFiles("*.sqlite", SearchOption.TopDirectoryOnly);
        if (saveSpecificFiles.Length is 0)
            return;
        var snapshotDirectory = new DirectoryInfo(Path.Combine(saveSpecificDirectory.FullName, saveSimNow.ToString("x16")));
        if (snapshotDirectory.Exists)
            foreach (var file in snapshotDirectory.GetFiles("*.sqlite", SearchOption.TopDirectoryOnly))
                file.Delete();
        else
            snapshotDirectory.Create();
        foreach (var file in saveSpecificFiles)
        {
            using var connection = new SqliteConnection($"Data Source={file.FullName}");
            await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            var com = connection.CreateCommand();
            com.CommandText = $"VACUUM INTO '{Path.Combine(snapshotDirectory.FullName, file.Name).Replace("'", "''", StringComparison.Ordinal)}';";
            await com.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }
        foreach (var invalidatedSnapshotDirectory in saveSpecificDirectory
            .GetDirectories("*.*", SearchOption.TopDirectoryOnly)
            .Select(directory => (directory, simNow: ulong.TryParse(directory.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var simNow) ? simNow : ulong.MaxValue))
            .Where(t => t.simNow > saveSimNow)
            .Select(t => t.directory))
            invalidatedSnapshotDirectory.Delete(true);
    }

    async Task WithRelationalDataStorageConnectionAsync(Guid uniqueId, bool isSaveSpecific, Func<SqliteConnection, Task> withSqliteConnectionAsyncAction, CancellationToken cancellationToken = default)
    {
        IDisposable? saveSpecificDataStoragePropagationLockHeld = null;
        if (isSaveSpecific)
            saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tcs = new TaskCompletionSource<DirectoryInfo>();
            StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.AppDataDirectory));
            var appDataDirectory = await tcs.Task.ConfigureAwait(false);
            var databaseDirectoryPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
            if (isSaveSpecific)
            {
                var saveSpecificDirectory = new DirectoryInfo(Path.Combine(databaseDirectoryPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}"));
                if (!saveSpecificDirectory.Exists)
                    saveSpecificDirectory.Create();
                databaseDirectoryPath = saveSpecificDirectory.FullName;
            }
            using var relationalDataStorageConnectionLockHeld = await relationalDataStorageConnectionLocks.GetOrAdd((uniqueId, isSaveSpecific), _ => new()).LockAsync(CancellationToken.None).ConfigureAwait(false);
            var databaseFile = new FileInfo(Path.Combine(databaseDirectoryPath, $"{uniqueId:n}.sqlite"));
            using var connection = new SqliteConnection($"Data Source={databaseFile.FullName}");
            await connection.OpenAsync(CancellationToken.None).ConfigureAwait(false);
            await withSqliteConnectionAsyncAction(connection).ConfigureAwait(false);
        }
        finally
        {
            saveSpecificDataStoragePropagationLockHeld?.Dispose();
        }
    }
}
