namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistSnapshotRow
{
    [Parameter]
    public Snapshot? Snapshot { get; set; }

    async Task CreateBranchAsync(Snapshot snapshot)
    {
        if (Archivist.SelectedChronicle is not { } chronicle
            || await DialogService.ShowCreateBranchDialogAsync(chronicle.Name, chronicle.Notes ?? string.Empty, chronicle.GameNameOverride ?? string.Empty, chronicle.Thumbnail).ConfigureAwait(false) is not { } createBranchDialogResult)
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.CallSplit, AppText.Archivist_Busy_Branching, "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.CreateBranchAsync(Settings, chronicle, createBranchDialogResult.ChronicleName, createBranchDialogResult.Notes, createBranchDialogResult.GameNameOverride, createBranchDialogResult.Thumbnail, taskCompletionSource.SetResult) is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
        else
        {
            taskCompletionSource.TrySetResult();
            await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, AppText.Archivist_Error_Text);
        }
    }

    async Task ExportSavePackageAsync(Snapshot snapshot)
    {
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileExport, AppText.Archivist_Busy_Exporting, "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.ExportSavePackageAsync(taskCompletionSource.SetResult) is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
    }

    async Task RestoreSavePackageAsync(Snapshot snapshot)
    {
        if (Archivist.SelectedChronicle is not { } chronicle
            || !await DialogService.ShowCautionDialogAsync(AppText.Archivist_Restore_Caution_Caption, AppText.Archivist_Restore_Caution_Text))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRestore, AppText.Archivist_Busy_Restoring, "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.RestoreSavePackageAsync(Settings, chronicle, taskCompletionSource.SetResult) is { } restoredFile)
            PlatformFunctions.ViewFile(restoredFile);
        else
        {
            taskCompletionSource.TrySetResult();
            await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, AppText.Archivist_Error_Text);
        }
    }
}
