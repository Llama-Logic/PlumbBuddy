namespace PlumbBuddy.Components.Layout;

[SuppressMessage("Maintainability", "CA1506: Avoid excessive class coupling")]
public partial class MainLayout
{
    static MudTheme CreatePlumbBuddyFactoryTheme() =>
        new()
        {
            PaletteLight = new PaletteLight()
            {
                AppbarBackground = "#594ae2a6",
                DrawerBackground = "#ffffffa6",
                Primary = "#00a2ffff",
                Surface = "#ffffffa6",
                Tertiary = "#74c044ff",
                Warning = "#d98806ff"
            },
            PaletteDark = new PaletteDark()
            {
                AppbarBackground = "#27272fa6",
                DrawerBackground = "#27272fa6",
                Primary = "#00a2ffff",
                Surface = "#373740a6",
                Tertiary = "#74c044ff",
                Warning = "#d98806ff"
            }
        };

    bool isDarkMode;
    bool isMainMenuDrawerOpen = false;
    DotNetObjectReference<MainLayout>? javaScriptThis;
    bool manualLightDarkModeToggleEnabled;
    bool? manualLightDarkModeToggle;
    bool themeManagerOpen = false;
    ThemeManagerTheme themeManagerTheme = new()
    {
        Theme = CreatePlumbBuddyFactoryTheme()
    };

    MudTheme Theme
    {
        get
        {
            if (Settings.ShowThemeManager)
                return themeManagerTheme.Theme;
            var factory = CreatePlumbBuddyFactoryTheme();
            ApplySettingsSelectedTheme(factory);
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

    void ApplySettingsSelectedTheme(MudTheme theme)
    {
        if (Settings.Theme is { } customThemeName && CustomThemes.Themes.TryGetValue(customThemeName, out var customTheme))
        {
            if (!string.IsNullOrWhiteSpace(customTheme.DefaultBorderRadius))
                theme.LayoutProperties.DefaultBorderRadius = customTheme.DefaultBorderRadius;
            if (!string.IsNullOrWhiteSpace(customTheme.Font))
            {
                string[] fontFamily =
                [
                    customTheme.Font,
                    "system-ui",
                    "-apple-system",
                    "Helvetica Neue",
                    "Helvetica",
                    "Arial",
                    "sans-serif"
                ];
                var typography = theme.Typography;
                typography.Body1.FontFamily = fontFamily;
                typography.Body2.FontFamily = fontFamily;
                typography.Button.FontFamily = fontFamily;
                typography.Caption.FontFamily = fontFamily;
                typography.Default.FontFamily = fontFamily;
                typography.H1.FontFamily = fontFamily;
                typography.H2.FontFamily = fontFamily;
                typography.H3.FontFamily = fontFamily;
                typography.H4.FontFamily = fontFamily;
                typography.H5.FontFamily = fontFamily;
                typography.H6.FontFamily = fontFamily;
                typography.Overline.FontFamily = fontFamily;
                typography.Subtitle1.FontFamily = fontFamily;
                typography.Subtitle2.FontFamily = fontFamily;
            }
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

    Task CloseDrawerHandler()
    {
        isMainMenuDrawerOpen = false;
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Settings.PropertyChanged -= HandleSettingsPropertyChanged;
            SuperSnacks.RefreshmentsOffered -= HandleSuperSnacksRefreshmentsOffered;
            javaScriptThis?.Dispose();
        }
    }

    bool? GetSettingsSelectedThemeIsDarkMode()
    {
        if (Settings.Theme is { } customThemeName && CustomThemes.Themes.TryGetValue(customThemeName, out var customTheme))
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

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.ShowThemeManager))
            StaticDispatcher.Dispatch(() =>
            {
                manualLightDarkModeToggleEnabled = false;
                manualLightDarkModeToggle = null;
                if (Application.Current is { } app)
                    SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
                StateHasChanged();
            });
        else if (e.PropertyName is nameof(ISettings.Theme))
            StaticDispatcher.Dispatch(() =>
            {
                SetPreferredColorScheme(GetSettingsSelectedThemeIsDarkMode() is { } themeIsDarkMode ? (themeIsDarkMode ? "dark" : "light") : Application.Current is { } app ? (app.RequestedTheme is AppTheme.Dark ? "dark" : "light") : string.Empty);
                StateHasChanged();
            });
    }

    void HandleSuperSnacksRefreshmentsOffered(object? sender, NummyEventArgs e) =>
        StaticDispatcher.Dispatch(() => Snackbar.Add(e.Message, e.Severity, e.Configure, e.Key));

    [JSInvokable]
    [SuppressMessage("Design", "CA1054: URI-like parameters should not be strings")]
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
            SuperSnacks.StopHoarding();
#if WINDOWS
            var appxPackagesDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages"));
            if (appxPackagesDirectory.Exists
                && appxPackagesDirectory.GetDirectories().Count(packageDirectory => packageDirectory.Name.StartsWith("com.llamalogic.plumbbuddy_", StringComparison.OrdinalIgnoreCase)) is > 1
                && await DialogService.ShowCautionDialogAsync
                (
                    "Multiple Installations of PlumbBuddy Detected",
                    """
                    You have more than one PlumbBuddy installation at the moment, and you really should *only have one.*

                    This is definitely not your fault.
                    Sometimes, when we need to update part of how we build and digitally sign PlumbBuddy for your protection, we change something which causes Windows to think of it as an entirely new app.
                    But PlumbBuddy is not designed to work properly with sibling installations of itself.
                    You really need to remove all but one of them.

                    We'd like to launch **Windows Settings** for you now so that you can type `PlumbBuddy` into the **Search apps** box and then remove all but the most recently installed version.
                    """
                ))
                await Windows.System.Launcher.LaunchUriAsync(new("ms-settings:appsfeatures"));
#endif
            if (!Settings.Onboarded)
                await DialogService.ShowOnboardingDialogAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        BlazorFramework.MainLayoutLifetimeScope = LifetimeScope;
        if (!StaticDispatcher.IsDispatcherSet)
            StaticDispatcher.RegisterDispatcher(Dispatcher);
        if (Application.Current is { } app)
            app.Windows[0].Title = "PlumbBuddy";
        if (Settings.ShowThemeManager)
            StateHasChanged();
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        SuperSnacks.RefreshmentsOffered += HandleSuperSnacksRefreshmentsOffered;
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        javaScriptThis = DotNetObjectReference.Create(this);
        if (Application.Current is { } app && app.Windows[0].Page is MainPage mainPage)
        {
            SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            await JSRuntime.InvokeVoidAsync("subscribeToPreferredColorSchemeChanges", javaScriptThis);
            await mainPage.ShowWebViewAsync();
        }
    }

    void OpenThemeManager(bool value) =>
        themeManagerOpen = value;

    void SetPreferredColorScheme(string colorScheme) =>
        isDarkMode = manualLightDarkModeToggle ?? GetSettingsSelectedThemeIsDarkMode() ?? colorScheme == "dark";

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
