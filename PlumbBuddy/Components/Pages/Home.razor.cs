namespace PlumbBuddy.Components.Pages;

partial class Home
{
    const int manfiestEditorTabIndex = 5;

    int activePanelIndex;
    DotNetObjectReference<Home>? javaScriptThis;
    bool keepPanelsAlive;

    public int ActivePanelIndex
    {
        get => activePanelIndex;
        set
        {
            if (activePanelIndex == value)
                return;
            if (value is manfiestEditorTabIndex)
                keepPanelsAlive = true;
            else if (activePanelIndex is manfiestEditorTabIndex && !ManifestEditor.RequestToRemainAlive)
                keepPanelsAlive = false;
            activePanelIndex = value;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Settings.PropertyChanged -= HandleSettingsPropertyChanged;
            UserInterfaceMessaging.BeginManifestingModRequested -= HandleUserInterfaceMessagingBeginManifestingModRequested;
            javaScriptThis?.Dispose();
        }
    }

    void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISettings.Theme))
            StaticDispatcher.Dispatch(() => _ = SetCustomThemeBackgroundsAsync());
        else if (e.PropertyName is nameof(ISettings.UiZoom))
            StaticDispatcher.Dispatch(() => _ = SetUiZoomAsync());
    }

    void HandleUserInterfaceMessagingBeginManifestingModRequested(object? sender, BeginManifestingModRequestedEventArgs e)
    {
        if (Settings.Type is UserType.Creator)
        {
            activePanelIndex = manfiestEditorTabIndex;
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Settings.PropertyChanged += HandleSettingsPropertyChanged;
        UserInterfaceMessaging.BeginManifestingModRequested += HandleUserInterfaceMessagingBeginManifestingModRequested;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        javaScriptThis = DotNetObjectReference.Create(this);
        await SetUiZoomAsync();
        await JSRuntime.InvokeVoidAsync("handleZoomFromDotNet", javaScriptThis, nameof(UserInvokedAccessibilityZoom));
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
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-dark", "url('/img/CatalogBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-light", "url('/img/CatalogBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-position", "center");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-dark", "url('/img/ModHoundBackground.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-light", "url('/img/ModHoundBackground.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-position", "right");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-repeat", "no-repeat");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-size", "contain");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-dark", "url('/img/ArchivistBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-light", "url('/img/ArchivistBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-position", "bottom");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-dark", "url('/img/ParlayBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-light", "url('/img/ParlayBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-position", "center");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-size", "cover");
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
                if ((modHealth?.TryGetValue("dark", out var dark) ?? false) && (modHealth?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
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
                if ((catalog?.TryGetValue("dark", out var dark) ?? false) && (catalog?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-catalog-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
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
            if (backgroundedTabs.TryGetValue("mod-hound", out var modHound))
            {
                if ((modHound?.TryGetValue("dark", out var dark) ?? false) && (catalog?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-dark", $"url('/img/custom-themes/{customThemeName}/mod-hound-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-light", $"url('/img/custom-themes/{customThemeName}/mod-hound-light.png')");
                }
                if ((modHound?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-position", position);
                if ((modHound?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-repeat", repeat);
                if ((modHound?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-hound-size", size);
            }
            if (backgroundedTabs.TryGetValue("archivist", out var archivist))
            {
                if ((archivist?.TryGetValue("dark", out var dark) ?? false) && (archivist?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-archivist-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
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
            if (backgroundedTabs.TryGetValue("parlay", out var parlay))
            {
                if ((parlay?.TryGetValue("dark", out var dark) ?? false) && (parlay?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-dark", $"url('/img/custom-themes/{customThemeName}/catalog-dark.png')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-light", $"url('/img/custom-themes/{customThemeName}/catalog-light.png')");
                }
                if ((parlay?.TryGetValue("position", out var position) ?? false) && !string.IsNullOrWhiteSpace(position))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-position", position);
                if ((parlay?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-repeat", repeat);
                if ((parlay?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-parlay-size", size);
            }
            if (backgroundedTabs.TryGetValue("manifest-editor", out var manifestEditor))
            {
                if ((manifestEditor?.TryGetValue("dark", out var dark) ?? false) && (manifestEditor?.TryGetValue("light", out var light) ?? false))
                {
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-dark", string.IsNullOrWhiteSpace(dark) ? "none" : $"url('/img/custom-themes/{customThemeName}/{dark}')");
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-light", string.IsNullOrWhiteSpace(light) ? "none" : $"url('/img/custom-themes/{customThemeName}/{light}')");
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

    async Task SetUiZoomAsync() =>
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-zoom", Settings.UiZoom.ToString());

    [JSInvokable]
    public void UserInvokedAccessibilityZoom(string command)
    {
        if (command == "in" && Settings.UiZoom <= 3.95M)
            Settings.UiZoom += 0.05M;
        else if (command == "out" && Settings.UiZoom >= 0.3M)
            Settings.UiZoom -= 0.05M;
        else if (command == "reset")
            Settings.UiZoom = 1;
    }
}
