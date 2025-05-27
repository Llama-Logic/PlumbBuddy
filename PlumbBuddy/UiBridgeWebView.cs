namespace PlumbBuddy;

public class UiBridgeWebView :
    WebView
{
    public UiBridgeWebView(ILogger<UiBridgeWebView> logger, ISettings settings, ZipArchive scriptModFile, string bridgedUiRootPath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scriptModFile);
        ArgumentNullException.ThrowIfNull(bridgedUiRootPath);
        Logger = logger;
        Settings = settings;
        ScriptModFile = scriptModFile;
        BridgedUiRootPath = bridgedUiRootPath;
        Source = "plumbbuddy://bridged-ui/index.html";
    }

    public string BridgedUiRootPath { get; }

    public ILogger<UiBridgeWebView> Logger { get; }

    public ISettings Settings { get; }

    public ZipArchive ScriptModFile { get; }
}