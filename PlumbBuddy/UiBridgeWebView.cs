namespace PlumbBuddy;

public partial class UiBridgeWebView :
    WebView
{
    static readonly Uri indexUrl = new($"{uriPrefix}index.html", UriKind.Absolute);
    static readonly JsonSerializerOptions messageSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
#if WINDOWS
    const string scheme = "http";
#else
    const string scheme = "plumbbuddy";
#endif
    const string uriPrefix = $"{scheme}://bridged-ui/";

    public UiBridgeWebView(ILogger<UiBridgeWebView> logger, ISettings settings, IUpdateManager updateManager, ZipArchive scriptModFile, string bridgedUiRootPath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(updateManager);
        ArgumentNullException.ThrowIfNull(scriptModFile);
        ArgumentNullException.ThrowIfNull(bridgedUiRootPath);
        Logger = logger;
        Settings = settings;
        this.updateManager = updateManager;
        this.scriptModFile = scriptModFile;
        this.bridgedUiRootPath = bridgedUiRootPath;
    }

    readonly string bridgedUiRootPath;
    readonly ZipArchive scriptModFile;
    readonly IUpdateManager updateManager;

    public ILogger<UiBridgeWebView> Logger { get; }

    public ISettings Settings { get; }

    string GetBridgedUiGatewayJavaScript()
    {
        using var customThemesMetadataStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("PlumbBuddy.Metadata.BridgedUiGateway.js")
            ?? throw new Exception("Bridged UI Gateway JavaScript embedded resource not found");
        using var customThemesMetadataStreamReader = new StreamReader(customThemesMetadataStream);
        return customThemesMetadataStreamReader.ReadToEnd().Replace("__PB_VERSION__", updateManager.CurrentVersion.ToString(), StringComparison.Ordinal);
    }

    public async ValueTask<(bool found, ReadOnlyMemory<byte> content, string contentType)> GetContentAsync(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
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
        using (var entryStream = entry.Open())
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
                ".JS" or ".MJS" => "application/javascript",
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
                ".WEBMANIFEST" => "application/manifest+json",
                ".WEBP" => "image/webp",
                ".WOFF" => "font/woff",
                ".WOFF2" => "font/woff2",
                ".XML" => "text/xml",
                _ => "application/octet-stream"
            }
        );
    }

    string GetMessageJson(object message) =>
        JsonSerializer.Serialize(message, messageSerializationOptions);

    private partial void InitializeWebView();

    private partial void Navigate(Uri uri);

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        InitializeWebView();
        Navigate(indexUrl);
    }

    void OnMessageFromBridgedUi(string message)
    {
        SendMessageToBridgedUi(new { DebugMessage = "Polo." });
    }

    private partial void SendMessageToBridgedUi(object message);
}