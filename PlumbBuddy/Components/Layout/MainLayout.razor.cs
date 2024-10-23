namespace PlumbBuddy.Components.Layout;

public partial class MainLayout
{
    static MudTheme CreatePlumbBuddyFactoryTheme() =>
        new()
        {
            PaletteLight = new PaletteLight()
            {
                //Error = "#ab273dff",
                Primary = "#594ae2ff",
                Tertiary = "#7bb56bff",
                Warning = "#d98806ff"
            },
            PaletteDark = new PaletteDark()
            {
                //Error = "#ab273dff",
                Primary = "#00a2ffff",
                Tertiary = "#7bb56bff",
                Warning = "#d98806ff"
            }
        };

    bool isDarkMode;
    bool isMainMenuDrawerOpen = false;
    DotNetObjectReference<MainLayout>? javaScriptThis;
    bool manualLightDarkModeToggleEnabled;
    bool? manualLightDarkModeToggle;
    int packageCount;
    int scriptArchiveCount;
    bool themeManagerOpen = false;
    ThemeManagerTheme themeManagerTheme = new()
    {
        Theme = CreatePlumbBuddyFactoryTheme()
    };

    MudTheme Theme
    {
        get
        {
            if (Player.ShowThemeManager)
                return themeManagerTheme.Theme;
            var factory = CreatePlumbBuddyFactoryTheme();
            ApplyPlayerSelectedTheme(factory);
            return factory;
        }
    }

    bool ManualLightDarkModeToggleEnabled
    {
        get => manualLightDarkModeToggleEnabled;
        set
        {
            if (value)
                manualLightDarkModeToggle = isDarkMode;
            manualLightDarkModeToggleEnabled = value;
            if (!value && Application.Current is { } app)
            {
                manualLightDarkModeToggle = null;
                SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            }
            StateHasChanged();
        }
    }

    bool ManualLightDarkModeToggle
    {
        get => manualLightDarkModeToggle ?? false;
        set
        {
            manualLightDarkModeToggle = value;
            SetPreferredColorScheme(string.Empty);
            StateHasChanged();
        }
    }

    Task CloseDrawerHandler()
    {
        isMainMenuDrawerOpen = false;
        return Task.CompletedTask;
    }

    void ApplyPlayerSelectedTheme(MudTheme theme)
    {
        if (Player.Theme is { } customThemeName && CustomThemes.Themes.TryGetValue(customThemeName, out var customTheme))
        {
            if (customTheme.PaletteLight is { } customThemeLightPaletteChanges)
                foreach (var (key, value) in customThemeLightPaletteChanges)
                    if (value is not null)
                        typeof(PaletteLight).GetProperty(key)?.SetValue(theme.PaletteLight, (MudBlazor.Utilities.MudColor)value);
            if (customTheme.PaletteDark is { } customThemeDarkPaletteChanges)
                foreach (var (key, value) in customThemeDarkPaletteChanges)
                    if (value is not null)
                        typeof(PaletteDark).GetProperty(key)?.SetValue(theme.PaletteDark, (MudBlazor.Utilities.MudColor)value);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SuperSnacks.RefreshmentsOffered -= HandleSuperSnacksRefreshmentsOffered;
        javaScriptThis?.Dispose();
    }

    bool? GetPlayerSelectedThemeIsDarkMode()
    {
        if (Player.Theme is { } customThemeName && CustomThemes.Themes.TryGetValue(customThemeName, out var customTheme))
        {
            var noLightPalette = customTheme.PaletteLight is null;
            var noDarkPalette = customTheme.PaletteDark is null;
            if (noLightPalette && noDarkPalette)
                return null;
            if (noDarkPalette)
                return false;
            if (noLightPalette)
                return true;
        }
        return null;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.CacheStatus))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
        else if (e.PropertyName is nameof(IPlayer.ShowThemeManager))
        {
            manualLightDarkModeToggleEnabled = false;
            manualLightDarkModeToggle = null;
            if (Application.Current is { } app)
                SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            StateHasChanged();
        }
        else if (e.PropertyName is nameof(IPlayer.Theme))
        {
            SetPreferredColorScheme(GetPlayerSelectedThemeIsDarkMode() is { } themeIsDarkMode ? (themeIsDarkMode ? "dark" : "light") : Application.Current is { } app ? (app.RequestedTheme is AppTheme.Dark ? "dark" : "light") : string.Empty);
            StateHasChanged();
        }
    }

    void HandleSuperSnacksRefreshmentsOffered(object? sender, NummyEventArgs e)
    {
        void nomNom() =>
            Snackbar.Add(e.Message, e.Severity, e.Configure, e.Key);
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(nomNom);
        else
            nomNom();
    }

    [JSInvokable]
    public async Task LaunchExternalUrlAsync(string url)
    {
        try
        {
            if (!await Browser.OpenAsync(url, BrowserLaunchMode.External))
                throw new Exception($"{nameof(Browser)}.{nameof(Browser.OpenAsync)} returned false");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "launching an external URL on the web view's behalf failed: {URL}", url);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("registerExternalLinkHandler", javaScriptThis);
            if (!Player.Onboarded)
                await DialogService.ShowOnboardingDialogAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        StaticDispatcher.RegisterDispatcher(Dispatcher);
        if (Application.Current is { } app)
            app.Windows[0].Title = "PlumbBuddy";
        if (Player.ShowThemeManager)
            StateHasChanged();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        SuperSnacks.RefreshmentsOffered += HandleSuperSnacksRefreshmentsOffered;
        packageCount = ModsDirectoryCataloger.PackageCount;
        scriptArchiveCount = ModsDirectoryCataloger.ScriptArchiveCount;
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        javaScriptThis = DotNetObjectReference.Create(this);
        if (Application.Current is { } app && app.MainPage is MainPage mainPage)
        {
            SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            await JSRuntime.InvokeVoidAsync("subscribeToPreferredColorSchemeChanges", javaScriptThis);
            await mainPage.ShowWebViewAsync();
        }
    }

    void OpenThemeManager(bool value) =>
        themeManagerOpen = value;

    void SetPreferredColorScheme(string colorScheme) =>
        isDarkMode = manualLightDarkModeToggle ?? GetPlayerSelectedThemeIsDarkMode() ?? colorScheme == "dark";

    /// <inheritdoc/>
    [JSInvokable]
    public void UpdatePreferredColorScheme(string colorScheme)
    {
        SetPreferredColorScheme(colorScheme);
        StateHasChanged();
    }

    void UpdateTheme(ThemeManagerTheme value)
    {
        themeManagerTheme = value;
        StateHasChanged();
    }
}
