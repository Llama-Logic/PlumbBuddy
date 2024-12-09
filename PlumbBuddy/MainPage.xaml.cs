
namespace PlumbBuddy;

[SuppressMessage("Maintainability", "CA1501: Avoid excessive inheritance")]
public partial class MainPage :
    ContentPage
{
    public static Microsoft.Maui.Graphics.Color TextColor =>
        Application.Current is { } app && app.RequestedTheme is AppTheme.Dark
            ? Microsoft.Maui.Graphics.Colors.White
            : Microsoft.Maui.Graphics.Colors.Black;

    public MainPage(ISettings settings, IAppLifecycleManager appLifecycleManager)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        this.settings = settings;
        this.appLifecycleManager = appLifecycleManager;
        InitializeComponent();
        BindingContext = this;
        UpdateTrayIconVisibility();
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ISettings settings;
    TaskbarIcon? trayIcon;
    bool webViewShownBefore;

    public bool ShowSystemTrayIcon =>
        settings.ShowSystemTrayIcon;

    public string LoadingLabel => appLifecycleManager.HideMainWindowAtLaunch
        ? AppText.DesktopInterface_Loading_HideMainWindow
        : AppText.DesktopInterface_Loading;

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ShowSystemTrayIcon))
            StaticDispatcher.Dispatch(UpdateTrayIconVisibility);
    }

    protected override void OnAppearing() =>
        settings.PropertyChanged += HandleSettingsPropertyChanged;

    protected override void OnDisappearing() =>
        settings.PropertyChanged -= HandleSettingsPropertyChanged;

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

    [RelayCommand]
    public void ShowWindow() =>
        appLifecycleManager.ShowWindow();

    [RelayCommand]
    public async Task Shutdown()
    {
        appLifecycleManager.ShowWindow();
        if (Application.Current is { } app
            && await DisplayAlert(AppText.MainMenu_Shutdown_Caution_Caption, AppText.MainMenu_Shutdown_Caution_Text, AppText.Common_Ok, AppText.Common_Cancel))
        {
            appLifecycleManager.PreventCasualClosing = false;
            app.CloseWindow(app.Windows[0]);
        }
    }

    void UpdateTrayIconVisibility()
    {
        if (settings.ShowSystemTrayIcon && trayIcon is null)
        {
            trayIcon = new()
            {
                IconSource = ImageSource.FromFile("plumbbuddy_icon.ico"),
                ToolTipText = "PlumbBuddy",
                LeftClickCommand = new Command(ShowWindow),
            };
            FlyoutBase.SetContextFlyout(trayIcon, new MenuFlyout
            {
                new MenuFlyoutItem
                {
                    Text = AppText.SystemTray_ContextMenu_ShowWindow,
                    Command = new Command(ShowWindow)
                },
                new MenuFlyoutSeparator(),
                new MenuFlyoutItem
                {
                    Text = AppText.MainMenu_Shutdown_Label,
                    Command = new Command(() => _ = Shutdown())
                }
            });
            pageContainer.Add(trayIcon);
        }
        else if (!settings.ShowSystemTrayIcon && trayIcon is not null)
        {
            pageContainer.Remove(trayIcon);
            trayIcon.Dispose();
            trayIcon = null;
        }
    }
}
