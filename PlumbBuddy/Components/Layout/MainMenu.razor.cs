namespace PlumbBuddy.Components.Layout;

partial class MainMenu
{
    int devToolsUnlockProgress = 10;
    bool devToolsUnlockProgressBadgeVisible = false;

    [Parameter]
    public EventCallback CloseDrawer { get; set; }

    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
    }

    async Task HandleAskForHelpOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        await DialogService.AskForHelpAsync(Logger, PublicCatalogs);
    }

    async Task HandleCheckForUpdateAsync()
    {
        await CloseDrawer.InvokeAsync();
        var (version, releaseNotes, downloadUrl) = await UpdateManager.CheckForUpdateAsync();
        if (version is not null)
            await UpdateManager.PresentUpdateAsync(version, releaseNotes, downloadUrl);
        else
            await DialogService.ShowInfoDialogAsync("PlumbBuddy is Up to Date", "You're running the most current stable version. 😁");
    }

    async Task HandleClearCacheOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.ClearCache();
    }

    async Task HandleCloseWindowOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        AppLifecycleManager.HideWindow();
    }

    Task HandleCloseMenuOnClickAsync() =>
        CloseDrawer.InvokeAsync();

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    async Task HandleOpenDownloadsFolderOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.OpenDownloadsFolder();
    }

    async Task HandleOpenModsFolderOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.OpenModsFolder();
    }

    async Task HandleOpenPlumbBuddyStorageOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        PlatformFunctions.ViewDirectory(new DirectoryInfo(FileSystem.AppDataDirectory));
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.CacheStatus))
            StaticDispatcher.Dispatch(StateHasChanged);
        else if (e.PropertyName == nameof(IPlayer.DevToolsUnlocked))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    async Task HandleReonboardOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (await DialogService.ShowCautionDialogAsync("Get Reacquainted?", "Going through that process again will reset all of your preferences. I will forget who Peter Par-- I mean-- you are. It will really be as though you just installed me for the first time, and we'll have to get to know each other all over again. Be sure that's what you want before you continue."))
        {
            Player.Forget(); // goodbye 😭
            await DialogService.ShowOnboardingDialogAsync();
        }
    }

    async Task HandleSettingsOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        await DialogService.ShowSettingsDialogAsync();
    }

    async Task HandleShutdownPlumbBuddyOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (Application.Current is { } app
            && await DialogService.ShowCautionDialogAsync("Are You Sure?", "I can't monitor your Mods folder while I'm shut down. You won't receive any alerts about potential problems until you start me up again."))
        {
            AppLifecycleManager.SignalShuttingDown();
            AppLifecycleManager.PreventCasualClosing = false;
            app.CloseWindow(app.Windows[0]);
        }
    }

    async Task HandleToggleThemeManagerOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (Player.ShowThemeManager)
            Player.ShowThemeManager = false;
        else if (await DialogService.ShowCautionDialogAsync("Toggle Theme Manager?", "Enabling the Theme Manager impacts PlumbBuddy's performance. You should probably only do this if you're 🍒."))
            Player.ShowThemeManager = true;
    }

    void HandleVersionOnClick()
    {
        if (Player.DevToolsUnlocked)
        {
            Player.DevToolsUnlocked = false;
            Player.ShowThemeManager = false;
            Snackbar.Add("Dev Tools locked. 🔒 I'll still help you with your mods, but please don't play with my heart. 🥲", Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HeartBroken);
        }
        else
        {
            --devToolsUnlockProgress;
            if (devToolsUnlockProgress == 9)
            {
                Snackbar.Add("Aww, I love you, too!", Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HeartPulse);
            }
            else if (devToolsUnlockProgress == 5)
            {
                devToolsUnlockProgressBadgeVisible = true;
                Snackbar.Add("🤚 Be careful, you're about to start a relationship.", Severity.Warning, options => options.Icon = MaterialDesignIcons.Normal.HeartHalfFull);
            }
            else if (devToolsUnlockProgress == 0)
            {
                Player.DevToolsUnlocked = true;
                devToolsUnlockProgressBadgeVisible = false;
                devToolsUnlockProgress = 10;
                Snackbar.Add("Marry me, you beautiful human. Dev Tools unlocked! 🔓", Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.Heart);
                Snackbar.Add("An inversion of control change just occurred which cannot take effect until the application is restarted.", Severity.Warning, options => options.Icon = MaterialDesignIcons.Normal.RestartAlert);
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged += HandlePlayerPropertyChanged;
    }
}
