using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;

namespace PlumbBuddy;

[SuppressMessage("Maintainability", "CA1501: Avoid excessive inheritance")]
public partial class UiBridgeWebView
{
    WebView2 PlatformWebView =>
        (WebView2)Handler!.PlatformView!;

    async void HandlerCoreWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        try
        {
            if (!Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var uri))
            {
                args.Response = sender.Environment.CreateWebResourceResponse
                (
                    Content: Stream.Null.AsRandomAccessStream(),
                    StatusCode: 404,
                    ReasonPhrase: "Not Found",
                    Headers: string.Empty
                );
                return;
            }
            var (found, content, contentType) = await GetContentAsync(uri);
            using var contentStream = new ReadOnlyMemoryOfByteStream(content);
#pragma warning disable CA2000 // Dispose objects before losing scope
            args.Response = found
                ? sender.Environment.CreateWebResourceResponse
                (
                    Content: contentStream.AsRandomAccessStream(),
                    StatusCode: 200,
                    ReasonPhrase: "OK",
                    Headers: $"Cache-Control: no-cache, max-age=0, must-revalidate, no-store\r\nContent-Length: {content.Length}\r\nContent-Type: {contentType}"
                ) : sender.Environment.CreateWebResourceResponse
                (
                    Content: new InMemoryRandomAccessStream(),
                    StatusCode: 404,
                    ReasonPhrase: "Not Found",
                    Headers: string.Empty
                );
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
        finally
        {
            deferral.Complete();
        }
    }

    void HandleCoreWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (args.TryGetWebMessageAsString() is { } message)
            OnMessageFromBridgedUi(message);
    }

    void HandleCoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
    {
        if (args.Exception is not null)
            return;
        var core = sender.CoreWebView2;
        // turn on Dev Tools for Amethyst, Lumpinou, and... everybody else
        core.Settings.AreDevToolsEnabled = Settings.Type is UserType.Creator;
        core.Settings.IsWebMessageEnabled = true;
        _ = core.AddScriptToExecuteOnDocumentCreatedAsync(GetBridgedUiGatewayJavaScript());
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebMessageReceived += HandleCoreWebMessageReceived;
        core.WebResourceRequested += HandlerCoreWebResourceRequested;
    }

    private partial void InitializeWebView() =>
        PlatformWebView.CoreWebView2Initialized += HandleCoreWebView2Initialized;

    private partial void Navigate(Uri uri) =>
        PlatformWebView.Source = uri;

    private partial void SendMessageToBridgedUi(string messageJson) =>
        PlatformWebView.CoreWebView2.PostWebMessageAsJson(messageJson);
}
