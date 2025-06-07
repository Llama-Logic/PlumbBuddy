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
        this.proxyHost.BridgedUiRequested += HandleProxyHostBridgedUiRequested;
        this.proxyHost.BridgedUiAuthorized += HandleProxyHostBridgedUiAuthorized;
        this.proxyHost.BridgedUiFocusRequested += HandleProxyHostBridgedUiFocusRequested;
        this.proxyHost.BridgedUiDestroyed += HandleProxyHostBridgedUiDestroyed;
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

    void HandleProxyHostBridgedUiAuthorized(object? sender, BridgedUiAuthorizedEventArgs e) =>
        _ = StaticDispatcher.DispatchAsync(async () =>
        {
            var webView = new UiBridgeWebView(lifetimeScope.Resolve<ILogger<UiBridgeWebView>>(), settings, lifetimeScope.Resolve<IUpdateManager>(), proxyHost, e.Archive, e.UiRoot, e.UniqueId);
            var tab = new SfTabItem { Content = webView, Header = e.TabName };
            ImageSource? imageSource = null;
            if (e.TabIconPath is { } tabIconPath
                && !string.IsNullOrWhiteSpace(tabIconPath))
            {
                if (e.Archive?.GetEntry(Path.Combine(e.UiRoot, tabIconPath).Replace("\\", "/", StringComparison.Ordinal)) is { } tabIconEntry)
                    imageSource = ImageSource.FromStream(() => e.Archive.GetInputStream(tabIconEntry));
                if (e.Archive is null && Uri.TryCreate(e.UiRoot, UriKind.Absolute, out var uiRootBaseUrl))
                    imageSource = ImageSource.FromUri(new(uiRootBaseUrl, tabIconPath));
            }
            tab.ImageSource = imageSource ?? "browser_window.png";
            tabView.Items.Add(tab);
            await RefreshTabViewAsync();
            tabView.SelectedIndex = tabView.Items.Count - 1;
        });

    void HandleProxyHostBridgedUiDestroyed(object? sender, BridgedUiEventArgs e) =>
        _ = StaticDispatcher.DispatchAsync(async () =>
        {
            if (tabView.Items.FirstOrDefault(t => t.Content is UiBridgeWebView webView && webView.UniqueId == e.UniqueId) is { } tabForDestroyedBridgedUi)
            {
                tabView.Items.Remove(tabForDestroyedBridgedUi);
                if (tabForDestroyedBridgedUi.Content is UiBridgeWebView uiBridgeWebView)
                    uiBridgeWebView.DisconnectHandlers();
                await RefreshTabViewAsync();
            }
        });

    void HandleProxyHostBridgedUiFocusRequested(object? sender, BridgedUiFocusRequestedEventArgs e) =>
        StaticDispatcher.Dispatch(() =>
        {
            if (tabView.Items.Select((tab, index) => (tab, index)).FirstOrDefault(t => t.tab.Content is UiBridgeWebView webView && webView.UniqueId == e.UniqueId) is { } tabToBeFocused)
            {
                var (_, index) = tabToBeFocused;
                if (index is <= 0)
                    return;
                appLifecycleManager.ShowWindow();
                tabView.SelectedIndex = index;
            }
        });

    void HandleProxyHostBridgedUiRequested(object? sender, BridgedUiRequestedEventArgs e) =>
        _ = StaticDispatcher.DispatchAsync(async () =>
        {
            await proxyHost.ForegroundPlumbBuddyAsync();
            if (await DisplayAlert("Bridged UI Requested",
                $"""
                {e.RequestorName} is asking for permission to show a bridged UI it wants to call {e.TabName}. If you allow this UI to be launched, it will be shown in my window and will be able to talk to mods in your game.

                The reason given is:
                {e.RequestReason}
                """, "Launch the UI", "Cancel"))
            {
                e.Authorize();
                return;
            }
            e.Deny();
        });

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

    async void HandleTabViewCenterButtonTapped(object sender, EventArgs e)
    {
        if (tabView.Items[tabView.SelectedIndex].Content is UiBridgeWebView)
        {
            tabViewRadialMenu.Opacity = 0.01;
            tabViewRadialMenu.IsVisible = true;
            await tabViewRadialMenu.FadeTo(1);
            tabViewRadialMenu.IsOpen = true;
            return;
        }
        await DisplayAlert("UI Bridge Control", "This button only has an effect when a bridged UI is the active tab.", "OK");
    }

    async void HandleTabViewRadialMenuClosed(object sender, Syncfusion.Maui.RadialMenu.ClosedEventArgs e)
    {
        await tabViewRadialMenu.FadeTo(0);
        tabViewRadialMenu.IsVisible = false;
    }

    async void HandleTabViewRadialMenuCloseItemTapped(object sender, Syncfusion.Maui.RadialMenu.ItemTappedEventArgs e)
    {
        if (!await DisplayAlert("UI Bridge Control: Closing", "If you continue, I will forcibly close this bridged UI and inform your mods that I did so.", "OK", "Cancel"))
            return;
        if (tabView.Items[tabView.SelectedIndex].Content is UiBridgeWebView uiBridgeWebView)
        {
            tabViewRadialMenu.IsOpen = false;
            proxyHost.DestroyBridgedUi(uiBridgeWebView.UniqueId);
        }
    }

    void HandleTabViewRadialMenuRefreshItemTapped(object sender, Syncfusion.Maui.RadialMenu.ItemTappedEventArgs e)
    {
        if (tabView.Items[tabView.SelectedIndex].Content is UiBridgeWebView uiBridgeWebView)
            uiBridgeWebView.Reload();
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

    async Task RefreshTabViewAsync()
    {
        tabView.IsCenterButtonEnabled = tabView.Items.Count is > 1;
        if (tabView.TabBarHeight == 0 && tabView.Items.Count is > 1)
        {
            var tcs = new TaskCompletionSource();
            using var animation = new Animation(v => tabView.TabBarHeight = v, 0, 80);
            animation.Commit(this, "Something", 16, 500, Easing.CubicInOut, (_, _) => tcs.SetResult());
            await tcs.Task;
        }
        else if (tabView.TabBarHeight == 80 && tabView.Items.Count is 1)
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
