#if WINDOWS
using H.NotifyIcon.Core;
using Microsoft.AspNetCore.Components.WebView.Maui;
#endif
using Syncfusion.Maui.Toolkit.TabView;
using Animation = Microsoft.Maui.Controls.Animation;

namespace PlumbBuddy;

[SuppressMessage("Maintainability", "CA1501: Avoid excessive inheritance")]
public partial class MainPage :
    ContentPage
{
    public static Microsoft.Maui.Graphics.Color OverlayColor =>
        Application.Current is { } app && app.RequestedTheme is AppTheme.Dark
            ? Microsoft.Maui.Graphics.Colors.Black
            : Microsoft.Maui.Graphics.Colors.White;

    public static Microsoft.Maui.Graphics.Color TextColor =>
        Application.Current is { } app && app.RequestedTheme is AppTheme.Dark
            ? Microsoft.Maui.Graphics.Colors.White
            : Microsoft.Maui.Graphics.Colors.Black;

    public MainPage(ILifetimeScope lifetimeScope, ISettings settings, IAppLifecycleManager appLifecycleManager, IUserInterfaceMessaging userInterfaceMessaging, IProxyHost proxyHost)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(appLifecycleManager);
        ArgumentNullException.ThrowIfNull(userInterfaceMessaging);
        ArgumentNullException.ThrowIfNull(proxyHost);
        this.lifetimeScope = lifetimeScope;
        this.settings = settings;
        this.appLifecycleManager = appLifecycleManager;
        this.userInterfaceMessaging = userInterfaceMessaging;
        this.proxyHost = proxyHost;
        InitializeComponent();
        BindingContext = this;
        ShowFileDropInterface = userInterfaceMessaging.IsFileDroppingEnabled;
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ILifetimeScope lifetimeScope;
    readonly IProxyHost proxyHost;
    readonly ISettings settings;
    bool showFileDropInterface;
#if WINDOWS
    TaskbarIcon? trayIcon;
#endif
    readonly IUserInterfaceMessaging userInterfaceMessaging;
    bool webViewShownBefore;

    public bool ShowFileDropInterface
    {
        get => showFileDropInterface;
        private set
        {
            if (showFileDropInterface == value)
                return;
            showFileDropInterface = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSystemTrayIcon =>
        settings.ShowSystemTrayIcon;

    public string LoadingLabel => appLifecycleManager.HideMainWindowAtLaunch
        ? AppText.DesktopInterface_Loading_HideMainWindow
        : AppText.DesktopInterface_Loading;

    void HandleCancelFileDropClicked(object sender, EventArgs e) =>
        userInterfaceMessaging.IsFileDroppingEnabled = false;

    void HandleProxyHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IProxyHost.IsClientConnected))
            _ = StaticDispatcher.DispatchAsync(RefreshTabViewAsync);
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
#if WINDOWS
        if (e.PropertyName is nameof(ISettings.ShowSystemTrayIcon))
            StaticDispatcher.Dispatch(UpdateTrayIconVisibility);
#endif
    }

    void HandleUserInterfaceMessagingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IUserInterfaceMessaging.IsFileDroppingEnabled))
            ShowFileDropInterface = userInterfaceMessaging.IsFileDroppingEnabled;
    }

    protected override void OnAppearing()
    {
        settings.PropertyChanged += HandleSettingsPropertyChanged;
        userInterfaceMessaging.PropertyChanged += HandleUserInterfaceMessagingPropertyChanged;
        proxyHost.PropertyChanged += HandleProxyHostPropertyChanged;
#if WINDOWS
        UpdateTrayIconVisibility();
#endif
    }

    protected override void OnDisappearing()
    {
        settings.PropertyChanged -= HandleSettingsPropertyChanged;
        userInterfaceMessaging.PropertyChanged -= HandleUserInterfaceMessagingPropertyChanged;
        proxyHost.PropertyChanged -= HandleProxyHostPropertyChanged;
#if WINDOWS
        if (trayIcon is not null)
        {
            pageContainer.Remove(trayIcon);
            trayIcon.Dispose();
            trayIcon = null;
        }
#endif
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        appLifecycleManager.WindowFirstShown(Window);
    }

    void AddATab()
    {
        var webView = new UiBridgeWebView(lifetimeScope.Resolve<ILogger<UiBridgeWebView>>(), settings, new ZipArchive(File.OpenRead("/Users/daniel/Desktop/bridged-ui.zip"), ZipArchiveMode.Read, false), "bridged-ui");
        var tab = new SfTabItem
        {
            Content = webView,
            Header = "Example Bridged UI"
        };
        tabView.Items.Add(tab);
    }

    async Task RefreshTabViewAsync()
    {
        if ((proxyHost.IsClientConnected || true) && tabView.TabBarHeight == 0)
        {
            AddATab();
            var tcs = new TaskCompletionSource();
            using var animation = new Animation(v => tabView.TabBarHeight = v, 0, 80);
            animation.Commit(this, "Something", 16, 500, Easing.CubicInOut, (_, _) => tcs.SetResult());
            await tcs.Task;
        }
        else if (!proxyHost.IsClientConnected && tabView.TabBarHeight == 80)
        {
            var tcs = new TaskCompletionSource();
            using var animation = new Animation(v => tabView.TabBarHeight = v, 80, 0);
            animation.Commit(this, "Something", 16, 500, Easing.CubicInOut, (_, _) => tcs.SetResult());
            await tcs.Task;
        }
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
        tabView.IsVisible = true;
        await Task.Delay(750);
        await Task.WhenAll(pleaseWait.FadeTo(0, 500), blazorWebView.FadeTo(1, 500));
        pleaseWait.IsVisible = false;
        await RefreshTabViewAsync();
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

    public Task DisplayJavaScriptErrorAsync(string errorJson) =>
        DisplayAlert("Un Unexpected UI Framework Error Has Occurred", errorJson, AppText.Common_Ok);

#if WINDOWS
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
#endif

    void HandleDragOver(object sender, DragEventArgs e)
    {
        if (e.PlatformArgs is { } args)
        {
#if WINDOWS
            e.AcceptedOperation = args.DragEventArgs.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
#endif
        }
    }

    async void HandleDrop(object sender, DropEventArgs e)
    {
#if WINDOWS
        if (e.PlatformArgs is { } args)
        {
            var dataView = args.DragEventArgs.DataView;
            if (dataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                userInterfaceMessaging.DropFiles((await dataView.GetStorageItemsAsync())
                    .OfType<Windows.Storage.IStorageItem>()
                    .Select(file => file.Path)
                    .ToImmutableArray());
        }
#else
        await Task.CompletedTask;
#endif
    }
}
