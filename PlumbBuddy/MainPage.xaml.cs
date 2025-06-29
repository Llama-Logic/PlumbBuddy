namespace PlumbBuddy;

[SuppressMessage("Maintainability", "CA1501: Avoid excessive inheritance")]
[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
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
        this.proxyHost.BridgedUiDomLoaded += HandleProxyHostBridgedUiDomLoaded;
        this.proxyHost.BridgedUiFocusRequested += HandleProxyHostBridgedUiFocusRequested;
        this.proxyHost.BridgedUiDestroyed += HandleProxyHostBridgedUiDestroyed;
        InitializeComponent();
        BindingContext = this;
        ShowFileDropInterface = userInterfaceMessaging.IsFileDroppingEnabled;
    }

    readonly IAppLifecycleManager appLifecycleManager;
    readonly ILifetimeScope lifetimeScope;
    readonly IProxyHost proxyHost;
    int selectedTabIndex;
    readonly ISettings settings;
    bool showFileDropInterface;
#if WINDOWS
    TaskbarIcon? trayIcon;
#endif
    readonly IUserInterfaceMessaging userInterfaceMessaging;
    bool webViewShownBefore;

    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set
        {
            if (value == selectedTabIndex)
                return;
            selectedTabIndex = value;
            OnPropertyChanged();
        }
    }

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

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase")]
    void HandleProxyHostBridgedUiAuthorized(object? sender, BridgedUiAuthorizedEventArgs e) =>
        StaticDispatcher.Dispatch(() =>
        {
            var webView = new UiBridgeWebView
            (
                lifetimeScope.Resolve<ILogger<UiBridgeWebView>>(),
                settings,
                lifetimeScope.Resolve<IUpdateManager>(),
                proxyHost,
                e.Archive,
                e.UiRoot,
                e.UniqueId,
                e.HostName ?? e.UniqueId.ToString("n").ToLowerInvariant()
            );
            var tab = new BottomTabItem
            {
                Label = e.TabName,
                IsEnabled = false
            };
            var reloadMenuItem = new MenuFlyoutItem { Text = "Reload" };
            reloadMenuItem.Clicked += (_, _) => webView.Refresh();
            var closeMenuItem = new MenuFlyoutItem { Text = "Close" };
            closeMenuItem.Clicked += async (_, _) =>
            {
                if (!await DisplayAlert("UI Bridge Control: Closing", "If you continue, I will forcibly close this bridged UI and inform your mods that I did so.", "OK", "Cancel"))
                    return;
                proxyHost.DestroyBridgedUi(e.UniqueId);
            };
            FlyoutBase.SetContextFlyout(tab, new MenuFlyout
            {
                reloadMenuItem,
                closeMenuItem
            });
            ImageSource? imageSource = null;
            if (e.TabIconPath is { } tabIconPath
                && !string.IsNullOrWhiteSpace(tabIconPath))
            {
                if (e.Archive?.GetEntry(Path.Combine(e.UiRoot, tabIconPath).Replace("\\", "/", StringComparison.Ordinal)) is { } tabIconEntry)
                {
                    var uiBridgeIconsDirectory = Directory.CreateDirectory(Path.Combine(MauiProgram.CacheDirectory.FullName, "UI Bridge Icons"));
                    var uiBridgeIconDirectory = Directory.CreateDirectory(Path.Combine(uiBridgeIconsDirectory.FullName, e.UniqueId.ToString("n")));
                    var uiBridgeIconFile = new FileInfo(Path.Combine(uiBridgeIconDirectory.FullName, tabIconEntry.Name.Split('/').Last()));
                    if (uiBridgeIconFile.Exists)
                        uiBridgeIconFile.Delete();
                    using (var uiBridgeIconFileStream = uiBridgeIconFile.OpenWrite())
                    {
                        using var inputStream = e.Archive.GetInputStream(tabIconEntry);
                        inputStream.CopyTo(uiBridgeIconFileStream);
                    }
                    imageSource = ImageSource.FromFile(uiBridgeIconFile.FullName);
                }
                if (e.Archive is null && Uri.TryCreate(e.UiRoot, UriKind.Absolute, out var uiRootBaseUrl))
                    imageSource = ImageSource.FromUri(new(uiRootBaseUrl, tabIconPath));
            }
            tab.IconImageSource = imageSource ?? "browser_window.png";
            viewSwitcher.Children.Add(webView);
            tabHostView.Tabs.Add(tab);
            RefreshTabHostView();
        });

    void HandleProxyHostBridgedUiDestroyed(object? sender, BridgedUiEventArgs e) =>
        StaticDispatcher.Dispatch(() =>
        {
            var bridgedUiIndex = viewSwitcher.Children.FindIndex(child => child is UiBridgeWebView webView && webView.UniqueId == e.UniqueId);
            if (bridgedUiIndex >= 1)
            {
                var tabContent = viewSwitcher.Children[bridgedUiIndex];
                if (tabContent is UiBridgeWebView uiBridgeWebView)
                    uiBridgeWebView.DisconnectHandlers();
                SelectedTabIndex = bridgedUiIndex - 1;
                viewSwitcher.Children.RemoveAt(bridgedUiIndex);
                tabHostView.Tabs.RemoveAt(bridgedUiIndex);
                RefreshTabHostView();
            }
        });

    void HandleProxyHostBridgedUiDomLoaded(object? sender, BridgedUiEventArgs e) =>
        StaticDispatcher.Dispatch(() =>
        {
            if (viewSwitcher.Children.Select((tab, index) => (tab, index)).FirstOrDefault(tuple => tuple.tab is UiBridgeWebView webView && webView.UniqueId == e.UniqueId) is { } tabAndIndex)
            {
                var (tab, index) = tabAndIndex;
                if (tab is not null)
                {
                    tabHostView.Tabs[index].IsEnabled = true;
                    SelectedTabIndex = index;
                }
            }
        });

    void HandleProxyHostBridgedUiFocusRequested(object? sender, BridgedUiFocusRequestedEventArgs e) =>
        StaticDispatcher.Dispatch(() =>
        {
            if (viewSwitcher.Children.Select((tab, index) => (tab, index)).FirstOrDefault(tuple => tuple.tab is UiBridgeWebView webView && webView.UniqueId == e.UniqueId) is { } tabToBeFocused)
            {
                var (_, index) = tabToBeFocused;
                if (index is <= 0)
                    return;
                appLifecycleManager.ShowWindow();
                SelectedTabIndex = index;
            }
        });

    void HandleProxyHostBridgedUiRequested(object? sender, BridgedUiRequestedEventArgs e) =>
        _ = StaticDispatcher.DispatchAsync(async () =>
        {
            await proxyHost.ForegroundPlumbBuddyAsync();
            if (await DisplayAlert("Bridged UI Requested",
                $"""
                {e.RequestorName} is asking for permission to show a bridged UI it wants to call {e.TabName}. If you allow this UI to be launched, it will be shown in my window and will be able to talk to mods in your game. After the UI is launched, you can right or secondary click on its tab to access its controls.

                The reason given is:
                {e.RequestReason}
                """, "Launch the UI", "Cancel"))
            {
                e.Authorize();
                return;
            }
            e.Deny();
        });

    void HandleProxyHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IProxyHost.IsClientConnected))
            _ = StaticDispatcher.DispatchAsync(RefreshTabHostView);
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

    void RefreshTabHostView()
    {
        if (!tabHostView.IsVisible && tabHostView.Tabs.Count is > 1)
            tabHostView.IsVisible = true;
        else if (tabHostView.IsVisible && tabHostView.Tabs.Count is 1)
            tabHostView.IsVisible = false;
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
        await Task.Delay(750);
        await Task.WhenAll(pleaseWait.FadeTo(0, 500), blazorWebView.FadeTo(1, 500));
        pleaseWait.IsVisible = false;
        RefreshTabHostView();
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

    //async void HandleTabViewSelectionChanged(object sender, TabSelectionChangedEventArgs e)
    //{
    //    var selectedTabContent = tabView.Items[tabView.SelectedIndex].Content;
    //    if (!selectedTabContent.IsVisible)
    //        selectedTabContent.IsVisible = true;
    //    if (selectedTabContent is UiBridgeWebView uiBridgeWebView
    //        && uiBridgeWebView.Opacity != 1)
    //        await uiBridgeWebView.FadeTo(1);
    //    await Task.Delay(TimeSpan.FromMilliseconds(tabView.ContentTransitionDuration));
    //    for (var i = 0; i < tabView.Items.Count; ++i)
    //        tabView.Items[i].Content.IsVisible = i == tabView.SelectedIndex;
    //}

    //void HandleTabViewSelectionChanging(object sender, SelectionChangingEventArgs e) =>
    //    tabView.Items[e.Index].Content.IsVisible = true;
}
