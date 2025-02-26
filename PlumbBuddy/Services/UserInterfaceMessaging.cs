namespace PlumbBuddy.Services;

public sealed class UserInterfaceMessaging :
    IUserInterfaceMessaging
{
    public UserInterfaceMessaging(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
    }

    readonly ISettings settings;

    bool isFileDroppingEnabled;

    public bool IsFileDroppingEnabled
    {
        get => isFileDroppingEnabled;
        set
        {
            if (isFileDroppingEnabled == value)
                return;
            isFileDroppingEnabled = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<BeginManifestingModRequestedEventArgs>? BeginManifestingModRequested;
    public event EventHandler<FilesDroppedEventArgs>? FilesDropped;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void BeginManifestingMod(string modFilePath) =>
        _ = StaticDispatcher.DispatchAsync(async () =>
        {
            BeginManifestingModRequested?.Invoke(this, new BeginManifestingModRequestedEventArgs
            {
                ModFilePath = modFilePath
            });
            await Task.Delay(TimeSpan.FromSeconds(1));
            BeginManifestingModRequested?.Invoke(this, new BeginManifestingModRequestedEventArgs
            {
                ModFilePath = modFilePath
            });
        });

    public void DropFiles(IReadOnlyList<string> paths) =>
        FilesDropped?.Invoke(this, new() { Paths = paths });

    public async Task<IReadOnlyList<string>> GetFilesFromDragAndDropAsync()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<string>>();
        void handleFilesDropped(object? sender, FilesDroppedEventArgs e) =>
            tcs.SetResult(e.Paths);
        void handlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IUserInterfaceMessaging.IsFileDroppingEnabled)
                && !IsFileDroppingEnabled)
                tcs.SetResult([]);
        }
        FilesDropped += handleFilesDropped;
        PropertyChanged += handlePropertyChanged;
        IsFileDroppingEnabled = true;
        var files = await tcs.Task;
        FilesDropped -= handleFilesDropped;
        PropertyChanged -= handlePropertyChanged;
        IsFileDroppingEnabled = false;
        return files;
    }

    public Task<bool> IsModScaffoldedAsync(string modFilePath)
    {
        return Task.FromResult<bool>(ManifestedModFileScaffolding.IsModFileScaffolded
        (
            new FileInfo(Path.Combine(settings.UserDataFolderPath, "Mods", modFilePath)),
            settings
        ));
    }

    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}
