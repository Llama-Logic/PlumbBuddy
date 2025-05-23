namespace PlumbBuddy.Components.Layout;

partial class MainMenu
{
    int devToolsUnlockProgress = 10;
    bool devToolsUnlockProgressBadgeVisible = false;

    [Parameter]
    public EventCallback CloseDrawer { get; set; }

    async Task HandleAskForHelpOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        await DialogService.ShowAskForHelpDialogAsync(Logger, PublicCatalogs);
    }

    async Task HandleCheckForUpdateAsync()
    {
        await CloseDrawer.InvokeAsync();
        var (version, releaseNotes, downloadUrl) = await UpdateManager.CheckForUpdateAsync();
        if (version is not null)
            await UpdateManager.PresentUpdateAsync(version, releaseNotes, downloadUrl);
        else
            await DialogService.ShowInfoDialogAsync(AppText.MainMenu_CheckForUpdate_UpToDate_Caption, AppText.MainMenu_CheckForUpdate_UpToDate_Text);
    }

    async Task HandleClearCacheOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        await SmartSimObserver.ClearCacheAsync();
    }

    async Task HandleCloseWindowOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        AppLifecycleManager.HideWindow();
    }

    Task HandleCloseMenuOnClickAsync() =>
        CloseDrawer.InvokeAsync();

    async Task HandleOpenDownloadsFolderOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        await SmartSimObserver.OpenDownloadsFolderAsync();
    }

    async Task HandleOpenModsFolderOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        SmartSimObserver.OpenModsFolder();
    }

    async Task HandleOpenPlumbBuddyCacheOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        PlatformFunctions.ViewDirectory(MauiProgram.CacheDirectory);
    }

    async Task HandleOpenPlumbBuddyStorageOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        PlatformFunctions.ViewDirectory(MauiProgram.AppDataDirectory);
    }

    async Task HandleReonboardOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (await DialogService.ShowCautionDialogAsync(AppText.MainMenu_DevTools_GetReacquainted_Caution_Caption, AppText.MainMenu_DevTools_GetReacquainted_Caution_Text))
        {
            Settings.Forget(); // goodbye 😭
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
            && await DialogService.ShowCautionDialogAsync(AppText.MainMenu_Shutdown_Caution_Caption, AppText.MainMenu_Shutdown_Caution_Text))
        {
            AppLifecycleManager.PreventCasualClosing = false;
            app.CloseWindow(app.Windows[0]);
        }
    }

    async Task HandleToggleThemeManagerOnClickAsync()
    {
        await CloseDrawer.InvokeAsync();
        if (Settings.ShowThemeManager)
            Settings.ShowThemeManager = false;
        else if (await DialogService.ShowCautionDialogAsync(AppText.MainMenu_DevTools_ToggleThemeManager_Caution_Caption, AppText.MainMenu_DevTools_ToggleThemeManager_Caution_Text))
        {
            Settings.Theme = null;
            Settings.ShowThemeManager = true;
        }
    }

    void HandleVersionOnClick()
    {
        if (Settings.DevToolsUnlocked)
        {
            Settings.DevToolsUnlocked = false;
            Settings.ShowThemeManager = false;
            Snackbar.Add(AppText.MainMenu_DevTools_Snack_Locked, Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HeartBroken);
        }
        else
        {
            --devToolsUnlockProgress;
            if (devToolsUnlockProgress == 9)
            {
                Snackbar.Add(AppText.MainMenu_DevTools_Snack_Unlocking_1, Severity.Normal, options => options.Icon = MaterialDesignIcons.Normal.HeartPulse);
            }
            else if (devToolsUnlockProgress == 5)
            {
                devToolsUnlockProgressBadgeVisible = true;
                Snackbar.Add(AppText.MainMenu_DevTools_Snack_Unlocking_2, Severity.Warning, options => options.Icon = MaterialDesignIcons.Normal.HeartHalfFull);
            }
            else if (devToolsUnlockProgress == 0)
            {
                Settings.DevToolsUnlocked = true;
                devToolsUnlockProgressBadgeVisible = false;
                devToolsUnlockProgress = 10;
                Snackbar.Add(AppText.MainMenu_DevTools_Snack_Unlocked, Severity.Success, options => options.Icon = MaterialDesignIcons.Normal.Heart);
                Snackbar.Add(AppText.MainMenu_DevTools_Snack_InversionOfControlChange, Severity.Warning, options => options.Icon = MaterialDesignIcons.Normal.RestartAlert);
            }
        }
    }
}
