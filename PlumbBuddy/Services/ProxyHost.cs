namespace PlumbBuddy.Services;

public partial class ProxyHost :
    IProxyHost
{
    const int port = 7342;
    static readonly JsonSerializerOptions options = new()
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
    };

    public ProxyHost()
    {
        cancellationTokenSource = new();
        cancellationToken = cancellationTokenSource.Token;
        connectedClients = new();
        listener = new(IPAddress.Loopback, port);
        listener.Start();
        _ = Task.Run(ListenAsync);
    }

    ~ProxyHost() =>
        Dispose(false);

    readonly CancellationTokenSource cancellationTokenSource;
    readonly CancellationToken cancellationToken;
    readonly ConcurrentDictionary<TcpClient, AsyncLock> connectedClients;
    bool isClientConnected;
    readonly TcpListener listener;

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

    public event EventHandler<ProxyHostClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ProxyHostClientConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<ProxyHostClientErrorEventArgs>? ClientError;
    public event EventHandler<ProxyHostMessageReceivedEventArgs>? MessageReceived;
    public event PropertyChangedEventHandler? PropertyChanged;

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
                    var messageData = JsonDocument.Parse(serializedMessageBuffer[..serializedMessageSize]);
                    MessageReceived?.Invoke(this, new()
                    {
                        Client = client,
                        Data = JsonDocument.Parse(serializedMessageBuffer[..serializedMessageSize])
                    });
                    if (messageData.RootElement.TryGetProperty("t", out var t)
                        && t.ValueKind == JsonValueKind.String
                        && t.GetString() is "control_message"
                        && messageData.RootElement.TryGetProperty("n", out var n)
                        && n.ValueKind == JsonValueKind.String
                        && n.GetString() is "game_services_stopped")
                        break;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(serializedMessageRentedArray);
                }
            }
            ClientDisconnected?.Invoke(this, new()
            {
                Client = client
            });
        }
        catch (OperationCanceledException)
        {
            ClientDisconnected?.Invoke(this, new()
            {
                Client = client
            });
        }
        catch (Exception ex)
        {
            ClientError?.Invoke(this, new()
            {
                Client = client,
                Exception = ex
            });
        }
        finally
        {
            if (connectedClients.TryRemove(client, out var clientWriteLock))
            {
                using var clientWriteLockHeld = await clientWriteLock.LockAsync(cancellationToken).ConfigureAwait(false);
                IsClientConnected = !connectedClients.IsEmpty;
                client.Dispose();
            }
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
                ClientConnected?.Invoke(this, new()
                {
                    Client = client
                });
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

    async Task SendMessageAsync(object message)
    {
        ObjectDisposedException.ThrowIf(cancellationToken.IsCancellationRequested, this);
        if (connectedClients.IsEmpty)
            return;
        var serializedMessageBytes = JsonSerializer.SerializeToUtf8Bytes(message, options);
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
                ClientError?.Invoke(this, new()
                {
                    Client = client,
                    Exception = ex
                });
                if (connectedClients.TryRemove(client, out var finallyClientWriteLock))
                {
                    using var clientWriteLockHeld = await finallyClientWriteLock.LockAsync(cancellationToken).ConfigureAwait(false);
                    IsClientConnected = !connectedClients.IsEmpty;
                    client.Dispose();
                }
            }
        }
    }
}
