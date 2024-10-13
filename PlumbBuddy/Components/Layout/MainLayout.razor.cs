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
        var paletteDark = theme.PaletteDark;
        if (Player.Theme is "Amethyst Lilac")
        {
            paletteDark.Primary = "#594ae2ff";
            paletteDark.Surface = "#342d6bff";
            paletteDark.Background = "#29226bff";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SuperSnacks.RefreshmentsOffered -= HandleSuperSnacksRefreshmentsOffered;
    }

    bool? GetPlayerSelectedThemeIsDarkMode()
    {
        if (Player.Theme is "Amethyst Lilac")
            return true;
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

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            if (!Player.Onboarded)
                await DialogService.ShowOnboardingDialogAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
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
        if (Application.Current is { } app && app.MainPage is MainPage mainPage)
        {
            SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            await JSRuntime.InvokeVoidAsync("subscribeToPreferredColorSchemeChanges", DotNetObjectReference.Create(this));
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
