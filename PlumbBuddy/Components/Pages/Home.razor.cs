namespace PlumbBuddy.Components.Pages;

partial class Home
{
    /// <inheritdoc />
    public void Dispose()
    {
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged -= HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged -= HandleSmartSimObserverPropertyChanged;
    }

    void HandleModsDirectoryCatalogerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IModsDirectoryCataloger.State))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(StateHasChanged);
            else
                StateHasChanged();
        }
    }

    void HandlePlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IPlayer.Theme))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(() => _ = SetCustomThemeBackgroundsAsync());
            else
                _ = SetCustomThemeBackgroundsAsync();
        }
        if (e.PropertyName is nameof(IPlayer.Type))
            StateHasChanged();
    }

    void HandleSmartSimObserverPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ISmartSimObserver.ScanIssues))
        {
            if (Dispatcher.IsDispatchRequired)
                Dispatcher.Dispatch(StateHasChanged);
            else
                StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ModsDirectoryCataloger.PropertyChanged += HandleModsDirectoryCatalogerPropertyChanged;
        Player.PropertyChanged += HandlePlayerPropertyChanged;
        SmartSimObserver.PropertyChanged += HandleSmartSimObserverPropertyChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await SetCustomThemeBackgroundsAsync();
    }

    async Task SetCustomThemeBackgroundsAsync()
    {
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-dark", "url('/img/ModHealthBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-light", "url('/img/ModHealthBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-size", "cover");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-dark", "url('/img/ManifestEditorBackgroundDark.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-light", "url('/img/ManifestEditorBackgroundLight.png')");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-repeat", "unset");
        await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-size", "cover");
        if (Player.Theme is { } customThemeName
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
                if ((modHealth?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-repeat", repeat);
                if ((modHealth?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-mod-health-size", size);
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
                if ((manifestEditor?.TryGetValue("repeat", out var repeat) ?? false) && !string.IsNullOrWhiteSpace(repeat))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-repeat", repeat);
                if ((manifestEditor?.TryGetValue("size", out var size) ?? false) && !string.IsNullOrWhiteSpace(size))
                    await JSRuntime.InvokeVoidAsync("setCssVariable", "--plumbbuddy-tab-background-manifest-editor-size", size);
            }
        }
    }
}
