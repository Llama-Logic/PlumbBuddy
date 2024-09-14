namespace PlumbBuddy.App.Components.Layout;

partial class MainMenu
{
    int devToolsUnlockProgress = 10;
    bool devToolsUnlockProgressBadgeVisible = false;

    [Parameter]
    public EventCallback CloseDrawer { get; set; }

    async Task ClearCacheHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.ClearCache();
    }

    async Task CloseWindowOnClickHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        AppLifecycleManager.HideWindow();
    }

    public void Dispose() =>
        Player.PropertyChanged -= HandlePlayerPropertyChanged;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Player.PropertyChanged += HandlePlayerPropertyChanged;
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IPlayer.DevToolsUnlocked))
            StateHasChanged();
    }

    void HandleVersionOnClick()
    {
        if (Player.DevToolsUnlocked)
        {
            Player.DevToolsUnlocked = false;
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
            }
        }
    }

    async Task OpenModsFolderHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.OpenModsFolder();
    }

    async Task ReonboardOnClickHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (await DialogService.ShowCautionDialogAsync("Get Reacquainted?", "Going through that process again will reset all of your preferences. I will forget who Peter Par-- I mean-- you are. It will really be as though you just installed me for the first time, and we'll have to get to know each other all over again. Be sure that's what you want before you continue."))
        {
            Player.Forget(); // goodbye 😭
            await DialogService.ShowOnboardingDialogAsync();
        }
    }

    async Task SettingsOnClickHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        await DialogService.ShowSettingsDialogAsync();
    }

    async Task ShutdownPlumbBuddyOnClickHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (Application.Current is { } app
            && await DialogService.ShowCautionDialogAsync("Are You Sure?", "I can't monitor your Mods folder while I'm shut down. You won't receive any alerts about potential problems until you start me up again."))
        {
            AppLifecycleManager.PreventCasualClosing = false;
            app.CloseWindow(app.Windows[0]);
        }
    }

    async Task ToggleThemeManagerOnClickHandlerAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (Player.ShowThemeManager)
            Player.ShowThemeManager = false;
        else if (await DialogService.ShowCautionDialogAsync("Toggle Theme Manager?", "Enabling the Theme Manager impacts PlumbBuddy's performance. You should probably only do this if you're 🍒."))
            Player.ShowThemeManager = true;
    }
}
