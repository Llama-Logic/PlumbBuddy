namespace PlumbBuddy.Components.Controls;

partial class ArchivistDisplay
{
    string chroniclesSearchText = string.Empty;
    bool isEditingChronicle;
    string snapshotsTextSearch = string.Empty;

    string ChroniclesSearchText
    {
        get => chroniclesSearchText;
        set
        {
            chroniclesSearchText = value;
            StateHasChanged();
        }
    }

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

    async Task ExportSavePackageAsync(Snapshot snapshot)
    {
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileExport, AppText.Archivist_Busy_Exporting, "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.ExportSavePackageAsync(taskCompletionSource.SetResult) is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
    }

    bool IncludeChronicle(Chronicle chronicle)
    {
        if (string.IsNullOrWhiteSpace(chroniclesSearchText))
            return true;
        if (chronicle.Name.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
            return true;
        if (chronicle.Notes?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        foreach (var snapshot in chronicle.Snapshots)
        {
            if (snapshot.ActiveHouseholdName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.Label.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (snapshot.LastPlayedLotName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.LastPlayedWorldName?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if (snapshot.LastWriteTime.ToString("g").Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
            if (snapshot.Notes?.Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            if ((snapshot.WasLive ? "Live" : string.Empty).Contains(chroniclesSearchText, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    bool IncludeSnapshot(Snapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshotsTextSearch))
            return true;
        if (snapshot.ActiveHouseholdName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.Label.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        if (snapshot.LastPlayedLotName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.LastPlayedWorldName?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if (snapshot.LastWriteTime.ToString("g").Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        if (snapshot.Notes?.Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;
        if ((snapshot.WasLive ? "Live" : string.Empty).Contains(snapshotsTextSearch, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    void NavigateToBasis(string? emphasis)
    {
        if (Archivist.SelectedChronicle is { } selectedChronicle
            && selectedChronicle.BasedOnSnapshot is { } basedOnSnapshot)
        {
            Archivist.SelectedChronicle = basedOnSnapshot.Chronicle;
            if (emphasis == "snapshot")
            {
                snapshotsTextSearch = basedOnSnapshot.LastWriteTime.ToString("g");
                basedOnSnapshot.ShowDetails = true;
            }
            else
                snapshotsTextSearch = string.Empty;
        }
    }

    async Task ReapplyEnhancementsAsync(Chronicle chronicle)
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.Archivist_ReapplyEnhancements_Caution_Caption, AppText.Archivist_ReapplyEnhancements_Caution_Text).ConfigureAwait(false))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        try
        {
            DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRefresh, AppText.Archivist_Busy_ReapplyingEnhancements, "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
            await Task.Run(async () => await Archivist.ReapplyEnhancementsAsync(chronicle).ConfigureAwait(false));
            taskCompletionSource.SetResult();
        }
        catch (Exception ex)
        {
            taskCompletionSource.SetResult();
            Logger.LogError(ex, "encountered unexpected unhandled exception while reapplying enhancements for nucleus ID {NucleusId}, created {Created}", chronicle.NucleusId, chronicle.Created);
            await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, AppText.Archivist_Error_Text);
        }
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

    async Task ShowChronicleDatabaseAsync(Chronicle chronicle)
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.Archivist_ShowChronicleDatabase_Caution_Caption, AppText.Archivist_ShowChronicleDatabase_Caution_Text))
            return;
        PlatformFunctions.ViewFile(new FileInfo(Path.Combine(Settings.ArchiveFolderPath, $"N-{chronicle.NucleusId:x16}-C-{chronicle.Created:x16}.chronicle.sqlite")));
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
