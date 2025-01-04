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

    async Task CreateBranchAsync(Snapshot snapshot)
    {
        if (Archivist.SelectedChronicle is { } chronicle
            && await DialogService.ShowCreateBranchDialogAsync(chronicle.Name, (uint)MemoryMarshal.Read<ulong>(chronicle.FullInstance.AsSpan())).ConfigureAwait(false) is { } createBranchDialogResult)
        {
            var taskCompletionSource = new TaskCompletionSource();
            DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.CallSplit, "Creating and Slotting First Save Package", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
            if (await snapshot.CreateBranchAsync(Settings, chronicle, createBranchDialogResult.ChronicleName, createBranchDialogResult.NewSaveGameInstance, taskCompletionSource.SetResult) is { } exportedFile)
                PlatformFunctions.ViewFile(exportedFile);
        }
    }

    async Task ExportModListAsync(Snapshot snapshot)
    {
        if (await snapshot.ExportModListAsync() is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
    }

    async Task ExportSavePackageAsync(Snapshot snapshot)
    {
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileExport, "Reconstructing and Exporting Save Package", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.ExportSavePackageAsync(taskCompletionSource.SetResult) is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
    }

    async Task RestoreSavePackageAsync(Snapshot snapshot)
    {
        if (Archivist.SelectedChronicle is { } chronicle)
        {
            var taskCompletionSource = new TaskCompletionSource();
            DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRestore, "Reconstructing and Slotting Save Package", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
            if (await snapshot.RestoreSavePackageAsync(Settings, chronicle, taskCompletionSource.SetResult) is { } restoredFile)
                PlatformFunctions.ViewFile(restoredFile);
        }
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
}
