namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistSnapshotDetails
{
    [Parameter]
    public Snapshot? Snapshot { get; set; }

    async Task DeletePreviousSnapshotsAsync(Snapshot snapshot)
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.Archivist_DeletePriorSnapshot_Caution_Caption, AppText.Archivist_DeletePriorSnapshot_Caution_Text))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.TimelineRemove, AppText.Archivist_Busy_DeletingPriorSnapshots, "json/archivist-deleting.json", "550px", "550px", taskCompletionSource.Task);
        await Task.Delay(TimeSpan.FromSeconds(5));
        taskCompletionSource.SetResult();
        if (!await DialogService.ShowCautionDialogAsync(AppText.Archivist_DeletePriorSnapshot_Caution2_Caption, AppText.Archivist_DeletePriorSnapshot_Caution2_Text))
            return;
        taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.TimelineRemove, AppText.Archivist_Busy_ActuallyDeletingPriorSnapshots, "json/archivist-deleting.json", "550px", "550px", taskCompletionSource.Task);
        await Task.WhenAll(Task.Delay(TimeSpan.FromSeconds(5)), snapshot.DeletePreviousSnapshotsAsync());
        taskCompletionSource.SetResult();
    }

    async Task ExportModListAsync(Snapshot snapshot)
    {
        if (await snapshot.ExportModListAsync() is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
    }

    async Task ShowSavePackageInSavesDirectoryAsync(Snapshot snapshot)
    {
        var savesFolder = new DirectoryInfo(Path.Combine(Settings.UserDataFolderPath, "saves"));
        if (!savesFolder.Exists)
            return;
        if (!(await DialogService.ShowQuestionDialogAsync(AppText.Archivist_ShowSavePackageInSavesDirectory_Question_Caption, AppText.Archivist_ShowSavePackageInSavesDirectory_Question_Text) ?? false))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileFind, AppText.Archivist_Busy_FindingSavePackageInSavesDirectory, "json/scan.json", "350px", "350px", taskCompletionSource.Task);
        FileInfo? foundFile = null;
        await Task.WhenAll(Task.Delay(TimeSpan.FromSeconds(3)), Task.Run(async () =>
        {
            foreach (var file in savesFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var sha256 = await ModFileManifestModel.GetFileSha256HashAsync(file.FullName).ConfigureAwait(false);
                    if (sha256.SequenceEqual(snapshot.OriginalPackageSha256)
                        || sha256.SequenceEqual(snapshot.EnhancedPackageSha256))
                    {
                        foundFile = file;
                        return;
                    }
                }
                catch
                {
                }
            }
        }));
        taskCompletionSource.SetResult();
        if (foundFile is not null)
        {
            PlatformFunctions.ViewFile(foundFile);
            return;
        }
        await DialogService.ShowInfoDialogAsync(AppText.Archivist_ShowSavePackageInSavesDirectory_NotFound_Caption, AppText.Archivist_ShowSavePackageInSavesDirectory_NotFound_Text);
    }
}
