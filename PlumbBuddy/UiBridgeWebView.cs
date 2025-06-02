namespace PlumbBuddy;

public partial class UiBridgeWebView :
    WebView
{
    static readonly Uri indexUrl = new($"{uriPrefix}index.html", UriKind.Absolute);
#if WINDOWS
    public const string Scheme = "http";
#else
    public const string Scheme = "plumbbuddy";
#endif
    const string uriPrefix = $"{Scheme}://bridged-ui/";

    public UiBridgeWebView(ILogger<UiBridgeWebView> logger, ISettings settings, IUpdateManager updateManager, IProxyHost proxyHost, ICSharpCode.SharpZipLib.Zip.ZipFile? scriptModFile, string bridgedUiRootPath, Guid uniqueId)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(updateManager);
        ArgumentNullException.ThrowIfNull(proxyHost);
        ArgumentNullException.ThrowIfNull(bridgedUiRootPath);
        Logger = logger;
        Settings = settings;
        UniqueId = uniqueId;
        this.updateManager = updateManager;
        this.proxyHost = proxyHost;
        this.proxyHost.BridgedUiDataSent += HandleProxyHostBridgedUiDataSent;
        this.proxyHost.BridgedUiMessageSent += HandleProxyHostMessageSent;
        this.scriptModFile = scriptModFile;
        this.bridgedUiRootPath = bridgedUiRootPath;
    }

    readonly string bridgedUiRootPath;
    readonly IProxyHost proxyHost;
    readonly ICSharpCode.SharpZipLib.Zip.ZipFile? scriptModFile;
    readonly IUpdateManager updateManager;

    public ILogger<UiBridgeWebView> Logger { get; }

    public ISettings Settings { get; }

    public Guid UniqueId { get; }

    public void DisconnectHandlers()
    {
        proxyHost.BridgedUiDataSent -= HandleProxyHostBridgedUiDataSent;
        proxyHost.BridgedUiMessageSent -= HandleProxyHostMessageSent;
    }

    void HandleProxyHostBridgedUiDataSent(object? sender, BridgedUiDataSentEventArgs e)
    {
        if (e.Recipient != UniqueId)
            return;
        SafeSendMessageToBridgedUi(e.MessageJson);
    }

    public string GetBridgedUiGatewayJavaScript()
    {
        using var customThemesMetadataStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("PlumbBuddy.Metadata.BridgedUiGateway.js")
            ?? throw new Exception("Bridged UI Gateway JavaScript embedded resource not found");
        using var customThemesMetadataStreamReader = new StreamReader(customThemesMetadataStream);
        return customThemesMetadataStreamReader.ReadToEnd()
            .Replace("__PB_VERSION__", updateManager.CurrentVersion.ToString(), StringComparison.Ordinal)
            .Replace("__UNIQUE_ID__", UniqueId.ToString(), StringComparison.Ordinal);
    }

    public async ValueTask<(bool found, ReadOnlyMemory<byte> content, string contentType)> GetContentAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (scriptModFile is null)
        {
            if (!Uri.TryCreate(bridgedUiRootPath, UriKind.Absolute, out var baseUri))
                return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
            using var httpClient = new HttpClient { BaseAddress = baseUri };
            var uriStr = uri.ToString()[uriPrefix.Length..];
            if (!string.IsNullOrEmpty(uri.Fragment))
                uriStr = uriStr[..^uri.Fragment.Length];
            using var response = await httpClient.GetAsync(uriStr).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "remote server did not return a successful status code when retrieving {UriStr}", uriStr);
                return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
            }
            var content = response.Content;
            using var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            var echoWriter = new ArrayBufferWriter<byte>();
            var rentedArray = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                Memory<byte> buffer = rentedArray;
                var bytesRead = await responseStream.ReadAsync(buffer).ConfigureAwait(false);
                while (bytesRead > 0)
                {
                    echoWriter.Write(buffer.Span[..bytesRead]);
                    bytesRead = await responseStream.ReadAsync(buffer).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
            return (true, echoWriter.WrittenMemory, content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        var entryName = uri.AbsoluteUri.ToString();
        if (!entryName.StartsWith(uriPrefix, StringComparison.Ordinal))
            return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
        entryName = entryName[uriPrefix.Length..];
        if (!string.IsNullOrEmpty(uri.Fragment))
            entryName = entryName[..^uri.Fragment.Length];
        if (!string.IsNullOrEmpty(uri.Query))
            entryName = entryName[..^uri.Query.Length];
        if (scriptModFile.GetEntry(Path.Combine(bridgedUiRootPath, entryName).Replace("\\", "/", StringComparison.Ordinal)) is not { } entry)
            return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
        var writer = new ArrayBufferWriter<byte>();
        using (var entryStream = scriptModFile.GetInputStream(entry))
        {
            var rentedArray = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                Memory<byte> buffer = rentedArray;
                var bytesRead = await entryStream.ReadAsync(buffer).ConfigureAwait(false);
                while (bytesRead > 0)
                {
                    writer.Write(buffer.Span[..bytesRead]);
                    bytesRead = await entryStream.ReadAsync(buffer).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "encountered unexpected unhandled exception while attempting to unpack {Entry} for bridged UI {UniqueId}", entryName, UniqueId);
                return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedArray);
            }
        }
        return
        (
            true,
            writer.WrittenMemory,
            Path.GetExtension(entryName).ToUpperInvariant() switch
            {
                ".CSS" => "text/css",
                ".GIF" => "image/gif",
                ".HTM" or ".HTML" => "text/html",
                ".ICO" => "image/x-icon",
                ".JPG" or ".JPEG" => "image/jpeg",
                ".JS" or ".MJS" => "text/javascript",
                ".JSON" or ".MAP" => "application/json",
                ".MP3" => "audio/mp3",
                ".MP4" => "video/mp4",
                ".OGG" => "audio/ogg",
                ".OTF" => "font/otf",
                ".PNG" => "image/png",
                ".SVG" => "image/svg+xml",
                ".TTF" => "font/ttf",
                ".TXT" => "text/plain",
                ".WASM" => "application/wasm",
                ".WAV" => "audio/wav",
                ".WEBP" => "image/webp",
                ".WOFF" => "font/woff",
                ".WOFF2" => "font/woff2",
                ".XML" => "text/xml",
                _ => "application/octet-stream"
            }
        );
    }

    void HandleProxyHostMessageSent(object? sender, BridgedUiMessageSentEventArgs e) =>
        SafeSendMessageToBridgedUi(e.MessageJson);

    private partial void InitializeWebView();

    private partial void Navigate(Uri uri);

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        InitializeWebView();
        if (scriptModFile is null)
        {
            Navigate(new Uri(uriPrefix, UriKind.Absolute));
            return;
        }
        Navigate(indexUrl);
    }

    public void OnMessageFromBridgedUi(string message) =>
        proxyHost.ProcessMessageFromBridgedUiAsync(UniqueId, message);

    void SafeSendMessageToBridgedUi(string messageJson) =>
        StaticDispatcher.Dispatch(() => SendMessageToBridgedUi(messageJson));

    private partial void SendMessageToBridgedUi(string messageJson);
}