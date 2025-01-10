namespace PlumbBuddy.Components.Pages;

partial class Home
{
    int activePanelIndex;
    bool keepPanelsAlive;

    public int ActivePanelIndex
    {
        get => activePanelIndex;
        set
        {
            if (activePanelIndex == value)
                return;
            if (value is 3)
                keepPanelsAlive = true;
            else if (activePanelIndex is 3 && !ManifestEditor.RequestToRemainAlive)
                keepPanelsAlive = false;
            activePanelIndex = value;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Settings.PropertyChanged -= HandleSettingsPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
        UserInterfaceMessaging.BeginManifestingModRequested -= HandleUserInterfaceMessagingBeginManifestingModRequested;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Theme))
            StaticDispatcher.Dispatch(() => _ = SetCustomThemeBackgroundsAsync());
        else if (e.PropertyName is nameof(ISettings.DevToolsUnlocked)
            or nameof(ISettings.ShowThemeManager)
            or nameof(ISettings.Type))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.ScanIssues))
            StaticDispatcher.Dispatch(StateHasChanged);
    }

    void HandleUserInterfaceMessagingBeginManifestingModRequested(object? sender, BeginManifestingModRequestedEventArgs e)
    {
        if (Settings.Type is UserType.Creator)
        {
            activePanelIndex = 2;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
        UserInterfaceMessaging.BeginManifestingModRequested += HandleUserInterfaceMessagingBeginManifestingModRequested;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await SetCustomThemeBackgroundsAsync();
    }

    [SuppressMessage("Maintainability", "CA1502: Avoid excessive complexity")]
    async Task SetCustomThemeBackgroundsAsync()
    {
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-dark", "url('/img/ModHealthBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-light", "url('/img/ModHealthBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-position", "center");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-dark", "url('/img/ArchivistBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-light", "url('/img/ArchivistBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-position", "bottom");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-dark", "url('/img/CatalogBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-light", "url('/img/CatalogBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-position", "center");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-dark", "url('/img/ManifestEditorBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-light", "url('/img/ManifestEditorBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-position", "center");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-size", "cover");
        if (Settings.Theme is { } customThemeName
            && CustomThemes.Themes.TryGetValue(customThemeName, out var customTheme)
            && customTheme.BackgroundedTabs is { } backgroundedTabs
            && backgroundedTabs.Count is > 0)
        {
            if (backgroundedTabs.TryGetValue("mod-health", out var modHealth))
            {
                if ((modHealth?.TryGetValue("dark", out var dark) ?? false) && !string.IsNullOrWhiteSpace(dark) && (modHealth?.TryGetValue("light", out var light) ?? false) && !string.IsNullOrWhiteSpace(light))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-dark", $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-light", $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-dark", $"url('/img/custom-themes/{customThemeName}/mod-health-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-light", $"url('/img/custom-themes/{customThemeName}/mod-health-light.png')");
                }
                if ((modHealth?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-position", position);
                if ((modHealth?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-repeat", repeat);
                if ((modHealth?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-size", size);
            }
            if (backgroundedTabs.TryGetValue("catalog", out var catalog))
            {
                if ((catalog?.TryGetValue("dark", out var dark) ?? false) && !string.IsNullOrWhiteSpace(dark) && (catalog?.TryGetValue("light", out var light) ?? false) && !string.IsNullOrWhiteSpace(light))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-dark", $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-light", $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-dark", $"url('/img/custom-themes/{customThemeName}/catalog-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-light", $"url('/img/custom-themes/{customThemeName}/catalog-light.png')");
                }
                if ((catalog?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-position", position);
                if ((catalog?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-repeat", repeat);
                if ((catalog?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-size", size);
            }
            if (backgroundedTabs.TryGetValue("archivist", out var archivist))
            {
                if ((archivist?.TryGetValue("dark", out var dark) ?? false) && !string.IsNullOrWhiteSpace(dark) && (archivist?.TryGetValue("light", out var light) ?? false) && !string.IsNullOrWhiteSpace(light))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-dark", $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-light", $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-dark", $"url('/img/custom-themes/{customThemeName}/catalog-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-light", $"url('/img/custom-themes/{customThemeName}/catalog-light.png')");
                }
                if ((archivist?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-position", position);
                if ((archivist?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-repeat", repeat);
                if ((archivist?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-size", size);
            }
            if (backgroundedTabs.TryGetValue("manifest-editor", out var manifestEditor))
            {
                if ((manifestEditor?.TryGetValue("dark", out var dark) ?? false) && !string.IsNullOrWhiteSpace(dark) && (manifestEditor?.TryGetValue("light", out var light) ?? false) && !string.IsNullOrWhiteSpace(light))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-dark", $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-light", $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-dark", $"url('/img/custom-themes/{customThemeName}/manifest-editor-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-light", $"url('/img/custom-themes/{customThemeName}/manifest-editor-light.png')");
                }
                if ((manifestEditor?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-position", position);
                if ((manifestEditor?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-repeat", repeat);
                if ((manifestEditor?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-size", size);
            }
        }
    }
}
