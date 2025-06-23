using Foundation;
using WebKit;

namespace PlumbBuddy;

public partial class UiBridgeWebView
{
    WKWebView PlatformWebView =>
        (WKWebView)Handler!.PlatformView!;

    private partial void InitializeWebView()
    {
        // no op
    }

    private partial void Navigate(Uri uri)
    {
        using var nsUrl = new NSUrl(uri.ToString());
        using var request = new NSUrlRequest(nsUrl);
        PlatformWebView.LoadRequest(request);
    }

    public partial void Refresh() =>
        PlatformWebView.Reload();

    private partial void SendMessageToBridgedUi(string messageJson) =>
        PlatformWebView.EvaluateJavaScriptAsync($"window.gateway.onMessageFromPlumbBuddy({messageJson});");
}