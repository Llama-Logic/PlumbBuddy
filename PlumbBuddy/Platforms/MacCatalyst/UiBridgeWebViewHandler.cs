using Foundation;
using Microsoft.Maui.Platform;
using System.Drawing;
using System.Runtime.Versioning;
using WebKit;

namespace PlumbBuddy;

partial class UiBridgeWebViewHandler
{
    protected override WKWebView CreatePlatformView()
    {
        var config = new WKWebViewConfiguration();
        config.SetUrlSchemeHandler(new SchemeHandler(this), urlScheme: "plumbbuddy");

        var contentController = new WKUserContentController();
        config.UserContentController = contentController;

        const string blockHttpJson =
            """
            [
                {
                    "trigger": {
                        "url-filter": "^(https?)://.*"
                    },
                    "action": {
                        "type": "block"
                    }
                }
            ]
            """;

        WKContentRuleListStore.DefaultStore.CompileContentRuleList
        (
            identifier: "BlockHttpAndHttps",
            encodedContentRuleList: blockHttpJson,
            completionHandler: (ruleList, error) =>
            {
                if (ruleList is not null)
                    contentController.AddContentRuleList(ruleList);
                else
                    UiBridgeWebView.Logger.LogWarning("Failed to block http and https due to this nonsense: {NSError}", error);
            }
        );

        // turn on Developer Extras for Frankk and Lot
        config.Preferences.SetValueForKey(NSObject.FromObject(UiBridgeWebView.Settings.Type is UserType.Creator), new NSString("developerExtrasEnabled"));

        var platformView = new MauiWKWebView(RectangleF.Empty, this, config);

        // turn on Safari Web Inspector for Frankk and Lot
        if (OperatingSystem.IsMacCatalystVersionAtLeast(major: 13, minor: 3))
            platformView.SetValueForKey(NSObject.FromObject(UiBridgeWebView.Settings.Type is UserType.Creator), new NSString("inspectable"));

        return platformView;
    }

    class SchemeHandler :
        NSObject,
        IWKUrlSchemeHandler
    {
        public SchemeHandler(UiBridgeWebViewHandler webViewHandler)
        {
            ArgumentNullException.ThrowIfNull(webViewHandler);
            this.webViewHandler = webViewHandler;
        }

        readonly UiBridgeWebViewHandler webViewHandler;
        
        [Export("webView:startURLSchemeTask:")]
        [SupportedOSPlatform("ios11.0")]
        public async void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
            if (!Uri.TryCreate(urlSchemeTask.Request.Url?.AbsoluteString, UriKind.Absolute, out var uri))
            {
                urlSchemeTask.DidFailWithError(new NSError((NSString)"PlumbBuddy", 404));
                return;
            }
            var (content, contentType) = await webViewHandler.GetContentAsync(uri);
            if (content.IsEmpty)
            {
                urlSchemeTask.DidFailWithError(new NSError((NSString)"PlumbBuddy", 404));
                return;
            }
            using (var dict = new NSMutableDictionary<NSString, NSString>())
            {
                dict.Add((NSString)"Content-Length", (NSString)content.Length.ToString(CultureInfo.InvariantCulture));
                dict.Add((NSString)"Content-Type", (NSString)contentType);
                dict.Add((NSString)"Cache-Control", (NSString)"no-cache, max-age=0, must-revalidate, no-store");
                if (urlSchemeTask.Request.Url is not null)
                {
                    using var response = new NSHttpUrlResponse(urlSchemeTask.Request.Url, 200, "HTTP/1.1", dict);
                    urlSchemeTask.DidReceiveResponse(response);
                }
            }
            urlSchemeTask.DidReceiveData(NSData.FromArray(content.ToArray()));
            urlSchemeTask.DidFinish();
        }

        [Export("webView:stopURLSchemeTask:")]
        public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
        }
    }
}