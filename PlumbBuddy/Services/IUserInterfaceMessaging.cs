namespace PlumbBuddy.Services;

public interface IUserInterfaceMessaging :
    INotifyPropertyChanged
{
    bool IsFileDroppingEnabled { get; set; }

    event EventHandler<BeginManifestingModRequestedEventArgs>? BeginManifestingModRequested;
    event EventHandler<FilesDroppedEventArgs>? FilesDropped;
    event EventHandler<IsModScaffoldedInquiredEventArgs>? IsModScaffoldedInquired;

    void BeginManifestingMod(string modFilePath);
    void DropFiles(IReadOnlyList<string> paths);
    Task<IReadOnlyList<string>> GetFilesFromDragAndDropAsync();
    Task<bool> IsModScaffoldedAsync(string modFilePath);
}
