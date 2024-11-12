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
        if (appLifecycleManager.HideMainWindowAtLaunch)
        {
            appLifecycleManager.HideWindow();
            _ = Task.Run(async () =>
            {
                await appLifecycleManager.UiReleaseSignal.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                appLifecycleManager.ShowWindow();
            });
        }
        await pleaseWait.FadeTo(0, 250);
        pleaseWait.IsVisible = false;
        blazorWebView.Opacity = 0;
        blazorWebView.IsVisible = true;
        await blazorWebView.FadeTo(0.01, 500);
        await blazorWebView.FadeTo(1, 500);
    }
}
