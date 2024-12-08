namespace PlumbBuddy.Services;

public interface IUserInterfaceMessaging
{
    event EventHandler<BeginManifestingModRequestedEventArgs>? BeginManifestingModRequested;
    event EventHandler<IsModScaffoldedInquiredEventArgs>? IsModScaffoldedInquired;

    void BeginManifestingMod(string modFilePath);
    Task<bool> IsModScaffoldedAsync(string modFilePath);
}
