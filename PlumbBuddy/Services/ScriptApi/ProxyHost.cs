using Epiforge.Extensions.Collections;

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
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
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
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
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

    public ProxyHost(ILogger<ProxyHost> logger, ISettings settings, IDbContextFactory<PbDbContext> pbDbContextFactory, IAppLifecycleManager appLifecycleManager)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pbDbContextFactory);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        this.logger = logger;
        this.settings = settings;
        this.pbDbContextFactory = pbDbContextFactory;
        this.appLifecycleManager = appLifecycleManager;
        this.settings.PropertyChanged += HandleSettingsPropertyChanged;
        cancellationTokenSource = new();
        cancellationToken = cancellationTokenSource.Token;
        connectedClients = new();
        bridgedUiLoadingLocks = new();
        loadedBridgedUis = new();
        relationalDataStorageConnectionInitializationLocks = new();
        saveSpecificDataStoragePropagationLock = new();
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
    ConcurrentDictionary<(Guid uniqueId, bool isSaveSpecific), AsyncLock> relationalDataStorageConnectionInitializationLocks;
    ulong saveCreated;
    ulong saveNucleusId;
    ulong saveSimNow;
    AsyncLock saveSpecificDataStoragePropagationLock;
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

    async Task<SqliteConnection> GetRelationalDataStorageConnectionAsync(Guid uniqueId, bool isSaveSpecific, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<DirectoryInfo>();
        StaticDispatcher.Dispatch(() => tcs.SetResult(MauiProgram.AppDataDirectory));
        var appDataDirectory = await tcs.Task.ConfigureAwait(false);
        var rdsPath = Path.Combine(appDataDirectory.FullName, "Relational Data Storage");
        Directory.CreateDirectory(rdsPath);
        if (isSaveSpecific)
        {
            var saveSpecificDirectory = new DirectoryInfo(Path.Combine(rdsPath, $"N-{saveNucleusId:x16}-C-{saveCreated:x16}"));
            if (!saveSpecificDirectory.Exists)
                saveSpecificDirectory.Create();
            using var saveSpecificDataStoragePropagationLockHeld = await saveSpecificDataStoragePropagationLock.LockAsync(cancellationToken).ConfigureAwait(false);
            var simNowDirectory = new DirectoryInfo(Path.Combine(saveSpecificDirectory.FullName, saveSimNow.ToString("x16")));
            if (!simNowDirectory.Exists)
            {
                simNowDirectory.Create();
                saveSpecificDirectory.Refresh();
                var allSimNowDirectories = saveSpecificDirectory
                    .GetDirectories("*.*", SearchOption.TopDirectoryOnly)
                    .Select(directory => (directory, simNow: ulong.TryParse(directory.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var simNow) ? simNow : ulong.MaxValue))
                    .OrderBy(t => t.simNow)
                    .ToImmutableArray();
                var currentIndex = allSimNowDirectories.FindIndex(t => t.directory.Name == simNowDirectory.Name);
                if (currentIndex > 0)
                {
                    var sourceDirectory = allSimNowDirectories[currentIndex - 1].directory;
                    var sourceFiles = sourceDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                    while (sourceFiles.Any(file => file.Extension.Equals(".wal", StringComparison.OrdinalIgnoreCase) || file.Extension.Equals(".shm", StringComparison.OrdinalIgnoreCase)))
                    {
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                        sourceFiles = sourceDirectory.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                    }
                    foreach (var file in sourceFiles)
                        file.CopyTo(Path.Combine(simNowDirectory.FullName, file.Name));
                }
            }
            rdsPath = simNowDirectory.FullName;
        }
        using var relationalDataStorageConnectionInitializationLockHeld = await relationalDataStorageConnectionInitializationLocks.GetOrAdd((uniqueId, isSaveSpecific), _ => new()).LockAsync(CancellationToken.None).ConfigureAwait(false);
        var databaseFile = new FileInfo(Path.Combine(rdsPath, $"{uniqueId:n}.sqlite"));
        var enableWriteAheadLogging = !databaseFile.Exists;
        var connection = new SqliteConnection($"Data Source={databaseFile.FullName}");
        await connection.OpenAsync(CancellationToken.None);
        if (enableWriteAheadLogging)
        {
            var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
            await pragmaCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }
        return connection;
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
            if (connectedClients.Count is 0)
            {
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

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Type))
            IsBridgedUiDevelopmentModeEnabled = false;
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
        if (archive.GetEntry(Path.Combine([..new string[] { requestMessage.UiRoot, "index.html" }.Where(segment => !string.IsNullOrWhiteSpace(segment))]).Replace("\\", "/", StringComparison.Ordinal)) is null)
        {
            await BridgedUiRequestResolvedAsync(requestMessage.UniqueId, BridgedUiRequestResponseMessage.DenialReason_IndexNotFound).ConfigureAwait(false);
            return;
        }
        PromptPlayerForBridgedUiAuthorization(requestMessage, archive);
    }

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
            case ComponentMessageType.GameServiceMessage:
                logger.LogDebug
                (
                    "game service message: {Name}",
                    messageRoot.TryGetProperty("name", out var nameProperty)
                    && nameProperty.ValueKind == JsonValueKind.String
                    && nameProperty.GetString() is { } namePropertyValue
                    ? namePropertyValue
                    : "[UNKNOWN]"
                );
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

    public async Task ShowInGameNotificationAsync(string text, string? title = null) =>
        await SendMessageToProxyAsync(new ShowNotificationMessage
        {
            Text = text,
            Title = title,
            Type = nameof(HostMessageType.ShowNotification).Underscore().ToLowerInvariant()
        }).ConfigureAwait(false);
}
