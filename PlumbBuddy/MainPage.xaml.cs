namespace PlumbBuddy;

[SuppressMessage("Maintainability", "CA1501: Avoid excessive inheritance")]
public partial class MainPage :
    ContentPage
{
    public static Microsoft.Maui.Graphics.Color TextColor =>
        Application.Current is { } app && app.RequestedTheme is AppTheme.Dark
            ? Microsoft.Maui.Graphics.Colors.White
            : Microsoft.Maui.Graphics.Colors.Black;

    public MainPage(IAppLifecycleManager appLifecycleManager)
    {
        this.appLifecycleManager = appLifecycleManager;
        InitializeComponent();
        BindingContext = this;
    }

    readonly IAppLifecycleManager appLifecycleManager;
    bool webViewShownBefore;

    public string LoadingLabel => appLifecycleManager.HideMainWindowAtLaunch
        ? "Starting Sims 4 Mods monitoring..."
        : "Just a moment, I'll will be right with you...";

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        appLifecycleManager.WindowFirstShown(Window);
    }

    public async Task ShowWebViewAsync()
    {
        if (webViewShownBefore)
            return;
        webViewShownBefore = true;
        if (appLifecycleManager.HideMainWindowAtLaunch)
        {
            appLifecycleManager.HideWindow();
            _ = Task.Run(async () =>
            {
                await appLifecycleManager.UiReleaseSignal.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                appLifecycleManager.ShowWindow();
            });
        }
        blazorWebView.Opacity = 0.01;
        blazorWebView.IsVisible = true;
        await Task.Delay(250);
        await Task.WhenAll(pleaseWait.FadeTo(0, 500), blazorWebView.FadeTo(1, 500));
        pleaseWait.IsVisible = false;
    }
}
