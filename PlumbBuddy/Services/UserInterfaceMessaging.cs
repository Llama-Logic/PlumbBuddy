namespace PlumbBuddy.Services;

public sealed class UserInterfaceMessaging :
    IUserInterfaceMessaging
{
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
    public event EventHandler<IsModScaffoldedInquiredEventArgs>? IsModScaffoldedInquired;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void BeginManifestingMod(string modFilePath) =>
        StaticDispatcher.Dispatch(() => BeginManifestingModRequested?.Invoke(this, new BeginManifestingModRequestedEventArgs
        {
            ModFilePath = modFilePath
        }));

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

    public async Task<bool> IsModScaffoldedAsync(string modFilePath)
    {
        var eventArgs = new IsModScaffoldedInquiredEventArgs
        {
            ModFilePath = modFilePath
        };
        foreach (var @delegate in IsModScaffoldedInquired?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
        {
            if (@delegate is EventHandler<IsModScaffoldedInquiredEventArgs> eventHandler)
            {
                var tcs = new TaskCompletionSource();
                StaticDispatcher.Dispatch(() =>
                {
                    eventHandler(this, eventArgs);
                    tcs.SetResult();
                });
                await tcs.Task.ConfigureAwait(false);
                if (eventArgs.IsModScaffolded is { } result)
                    return result;
            }
        }
        return false;
    }
    void OnPropertyChanged(PropertyChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, e);

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}
