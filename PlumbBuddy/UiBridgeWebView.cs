namespace PlumbBuddy;

public partial class UiBridgeWebView :
    WebView
{
#if WINDOWS
    public const string Scheme = "http";
#else
    public const string Scheme = "plumbbuddy";
#endif

    public UiBridgeWebView(ILogger<UiBridgeWebView> logger, ISettings settings, IUpdateManager updateManager, IProxyHost proxyHost, IGameResourceCataloger gameResourceCataloger, ZipFile? scriptModFile, string bridgedUiRootPath, Guid uniqueId, string hostName, IReadOnlyList<(ZipFile? archive, string uiRoot)> layers)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(updateManager);
        ArgumentNullException.ThrowIfNull(proxyHost);
        ArgumentNullException.ThrowIfNull(gameResourceCataloger);
        ArgumentNullException.ThrowIfNull(bridgedUiRootPath);
        ArgumentNullException.ThrowIfNull(hostName);
        ArgumentNullException.ThrowIfNull(layers);
        Logger = logger;
        Settings = settings;
        UniqueId = uniqueId;
        this.updateManager = updateManager;
        this.proxyHost = proxyHost;
        this.proxyHost.BridgedUiDataSent += HandleProxyHostBridgedUiDataSent;
        this.proxyHost.BridgedUiMessageSent += HandleProxyHostMessageSent;
        this.proxyHost.SpecificBridgedUiMessageSent += HandleProxyHostSpecificBridgedUiMessageSent;
        this.gameResourceCataloger = gameResourceCataloger;
        this.hostName = hostName;
        this.layers = layers.Append((scriptModFile, bridgedUiRootPath)).Reverse().ToList().AsReadOnly();
    }

    readonly IGameResourceCataloger gameResourceCataloger;
    readonly string hostName;
    readonly IReadOnlyList<(ZipFile? archive, string uiRoot)> layers;
    readonly IProxyHost proxyHost;
    readonly IUpdateManager updateManager;

    Uri IndexUrl =>
        new($"{UriPrefix}index.html", UriKind.Absolute);

    public ILogger<UiBridgeWebView> Logger { get; }

    public ISettings Settings { get; }

    public Guid UniqueId { get; }

    string UriPrefix =>
        $"{Scheme}://{hostName}/";

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

    [SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
    public async ValueTask<(bool found, ReadOnlyMemory<byte> content, string contentType)> GetContentAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var entryName = uri.AbsoluteUri.ToString();
        entryName = entryName[UriPrefix.Length..];
        if (!string.IsNullOrEmpty(uri.Fragment))
            entryName = entryName[..^uri.Fragment.Length];
        if (!string.IsNullOrEmpty(uri.Query))
            entryName = entryName[..^uri.Query.Length];
        if (entryName.StartsWith("game-environment/", StringComparison.OrdinalIgnoreCase))
        {
            entryName = entryName["game-environment/".Length..];
            if (entryName.StartsWith("resources/", StringComparison.OrdinalIgnoreCase))
            {
                entryName = entryName["resources/".Length..];
                var resource = await gameResourceCataloger.GetRawResourceAsync(Uri.UnescapeDataString(entryName)).ConfigureAwait(false);
                return (!resource.IsEmpty, resource, resource.IsEmpty ? string.Empty : "application/octet-stream");
            }
            if (entryName.StartsWith("screenshots/", StringComparison.OrdinalIgnoreCase))
            {
                entryName = entryName["screenshots/".Length..];
                var screenshot = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "Screenshots", Uri.UnescapeDataString(entryName)));
                if (!screenshot.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
                if (!screenshot.Exists)
                    screenshot = new(Path.Combine(Settings.UserDataFolderPath, "Screenshots", Uri.UnescapeDataString(entryName).Replace((char)32, (char)160)));
                if (!screenshot.Exists)
                    return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
                using var screenshotStream = new ArrayBufferWriterOfByteStream();
                try
                {
                    using (var screenshotFileStream = screenshot.OpenRead())
                        await screenshotFileStream.CopyToAsync(screenshotStream).ConfigureAwait(false);
                    return (true, screenshotStream.WrittenMemory, "image/png");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "unhandled exception while loading screenshot");
                    return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
                }
            }
            return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
        }
        foreach (var (scriptModFile, bridgedUiRootPath) in layers)
        {
            if (scriptModFile is null)
            {
                if (!Uri.TryCreate(bridgedUiRootPath, UriKind.Absolute, out var baseUri))
                    continue;
                using var httpClient = new HttpClient { BaseAddress = baseUri };
                var uriStr = uri.ToString()[UriPrefix.Length..];
                if (!string.IsNullOrEmpty(uri.Fragment))
                    uriStr = uriStr[..^uri.Fragment.Length];
                using var response = await httpClient.GetAsync(uriStr).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    continue;
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
            if (!uri.AbsoluteUri.ToString().StartsWith(UriPrefix, StringComparison.Ordinal))
                continue;
            if (scriptModFile.GetEntry(Path.Combine(bridgedUiRootPath, Uri.UnescapeDataString(entryName)).Replace("\\", "/", StringComparison.Ordinal)) is not { } entry)
                continue;
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
                    Logger.LogWarning(ex, "encountered unexpected unhandled exception while attempting to unpack {Entry} for bridged UI {UniqueId}", entryName, UniqueId);
                    continue;
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
        return (false, ReadOnlyMemory<byte>.Empty, string.Empty);
    }

    void HandleProxyHostBridgedUiDataSent(object? sender, BridgedUiDataSentEventArgs e)
    {
        if (e.Recipient != UniqueId)
            return;
        SendMessageToBridgedUi(e.MessageJson);
    }

    void HandleProxyHostMessageSent(object? sender, BridgedUiMessageSentEventArgs e) =>
        SendMessageToBridgedUi(e.MessageJson);

    void HandleProxyHostSpecificBridgedUiMessageSent(object? sender, SpecificBridgedUiMessageSentEventArgs e)
    {
        if (e.UniqueId != UniqueId)
            return;
        SendMessageToBridgedUi(e.MessageJson);
    }

    private partial void InitializeWebView();

    private partial void Navigate(Uri uri);

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        InitializeWebView();
        if (layers[0].archive is null)
        {
            Navigate(new Uri(UriPrefix, UriKind.Absolute));
            return;
        }
        Navigate(IndexUrl);
    }

    public void OnMessageFromBridgedUi(string message) =>
        proxyHost.ProcessMessageFromBridgedUiAsync(UniqueId, message);

    public partial void Refresh();

    private partial void SendMessageToBridgedUi(string messageJson);

    private partial void Shutdown();

    public void Unload()
    {
        proxyHost.BridgedUiDataSent -= HandleProxyHostBridgedUiDataSent;
        proxyHost.BridgedUiMessageSent -= HandleProxyHostMessageSent;
        proxyHost.SpecificBridgedUiMessageSent -= HandleProxyHostSpecificBridgedUiMessageSent;
        Shutdown();
    }
}