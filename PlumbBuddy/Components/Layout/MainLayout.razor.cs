namespace PlumbBuddy.Components.Layout;

public partial class MainLayout
{
    static MudTheme CreatePlumbBuddyFactoryTheme() =>
        new()
        {
            PaletteLight = new PaletteLight()
            {
                Error = "#ab273dff",
                Primary = "#594ae2ff",
                Tertiary = "#7bb56bff",
                Warning = "#d98806ff"
            },
            PaletteDark = new PaletteDark()
            {
                Error = "#ab273dff",
                Primary = "#00a2ffff",
                Tertiary = "#7bb56bff",
                Warning = "#d98806ff"
            }
        };

    bool manualLightDarkModeToggleEnabled;
    bool? manualLightDarkModeToggle;
    int packageCount;
    int scriptArchiveCount;
    readonly MudTheme theme = CreatePlumbBuddyFactoryTheme();
    bool themeManagerOpen = false;
    ThemeManagerTheme themeManagerTheme = new()
    {
        Theme = CreatePlumbBuddyFactoryTheme()
    };

    [Inject]
    IJSRuntime JSRuntime { get; set; } = default!;

    bool isDarkMode;
    bool isMainMenuDrawerOpen = false;

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

    /// <inheritdoc/>
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged -= HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
        SuperSnacks.RefreshmentsOffered -= HandleSuperSnacksRefreshmentsOffered;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is (nameof(IModsDirectoryCataloger.State))
            or (nameof(IModsDirectoryCataloger.PackageCount))
            or (nameof(IModsDirectoryCataloger.ResourceCount))
            or (nameof(IModsDirectoryCataloger.ScriptArchiveCount)))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.IsModsDisabledGameSettingOn)
            or nameof(ISmartSimObserver.IsScriptModsEnabledGameSettingOn))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
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

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Player.CacheStatus))
        {
            if (!Dispatcher.IsDispatchRequired)
                StateHasChanged();
            else
                Dispatcher.Dispatch(StateHasChanged);
        }
        else if (e.PropertyName is nameof(Player.ShowThemeManager))
        {
            manualLightDarkModeToggleEnabled = false;
            manualLightDarkModeToggle = null;
            if (Application.Current is { } app)
                SetPreferredColorScheme(app.RequestedTheme is AppTheme.Dark ? "dark" : "light");
            StateHasChanged();
        }
        else if (e.PropertyName is nameof(Player.Type))
            StateHasChanged();
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
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
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
        isDarkMode = manualLightDarkModeToggle ?? colorScheme == "dark";

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
