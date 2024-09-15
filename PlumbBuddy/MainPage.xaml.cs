namespace PlumbBuddy;

public partial class MainPage :
    ContentPage
{
    public static Microsoft.Maui.Graphics.Color TextColor =>
        Application.Current is { } app && app.RequestedTheme is AppTheme.Dark
            ? Microsoft.Maui.Graphics.Colors.White
            : Microsoft.Maui.Graphics.Colors.Black;

    public MainPage(IAppLifecycleManager appLifecycleManager)
    {
        InitializeComponent();
        this.appLifecycleManager = appLifecycleManager;
    }

    readonly IAppLifecycleManager appLifecycleManager;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        appLifecycleManager.WindowFirstShown(Window);
    }

    public async Task ShowWebViewAsync()
    {
        await pleaseWait.FadeTo(0, 250);
        pleaseWait.IsVisible = false;
        blazorWebView.Opacity = 0;
        blazorWebView.IsVisible = true;
        await blazorWebView.FadeTo(0.01, 500);
        await blazorWebView.FadeTo(1, 500);
    }
}
