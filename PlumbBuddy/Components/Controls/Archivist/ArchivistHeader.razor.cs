namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistHeader
{
    async Task BrowseForFolderToScanAsync()
    {
        if (await FolderPicker.Default.PickAsync().ConfigureAwait(false) is not { } folderPickerResult
            || !folderPickerResult.IsSuccessful)
            return;
        var (folder, pickerEx) = folderPickerResult;
        if (pickerEx is not null)
        {
            Logger.LogError(pickerEx, "encountered unexpected unhandled exception when picking a folder to scan");
            await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, $"{pickerEx.GetType().Name}: {pickerEx.Message}").ConfigureAwait(false);
            return;
        }
        if (folder is null)
            return;
        await Archivist.AddPathToProcessAsync(new DirectoryInfo(folder.Path));
    }
}
