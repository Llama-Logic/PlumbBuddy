using Foundation;
using WebKit;

namespace PlumbBuddy;

public partial class UiBridgeWebView
{
    bool isDisposed;

    WKWebView PlatformWebView =>
        (WKWebView)Handler!.PlatformView!;

    private partial void InitializeWebView()
    {
        // no op
    }

    private partial void Navigate(Uri uri) =>
        StaticDispatcher.Dispatch(() =>
        {
            if (isDisposed)
                return;
            using var nsUrl = new NSUrl(uri.ToString());
            using var request = new NSUrlRequest(nsUrl);
            PlatformWebView.LoadRequest(request);
        });

    public partial void Refresh() =>
        StaticDispatcher.Dispatch(() =>
        {
            if (isDisposed)
                return;
            PlatformWebView.Reload();
        });

    private partial void SendMessageToBridgedUi(string messageJson) =>
        StaticDispatcher.Dispatch(() =>
        {
            if (isDisposed)
                return;
            PlatformWebView.EvaluateJavaScriptAsync($"window.gateway.onMessageFromPlumbBuddy({messageJson});");
        });

    private partial void Shutdown() =>
        StaticDispatcher.Dispatch(() =>
        {
            if (isDisposed)
                return;
            PlatformWebView.StopLoading();
            PlatformWebView.RemoveFromSuperview();
            PlatformWebView.Dispose();
            isDisposed = true;
        });
}