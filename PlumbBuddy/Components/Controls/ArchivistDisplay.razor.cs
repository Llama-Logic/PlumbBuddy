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
            await DialogService.ShowErrorDialogAsync("Something Went Wrong", $"{pickerEx.GetType().Name}: {pickerEx.Message}").ConfigureAwait(false);
            return;
        }
        if (folder is null)
            return;
        await Archivist.AddPathToProcessAsync(new DirectoryInfo(folder.Path));
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
        if (!await DialogService.ShowCautionDialogAsync("This Might Take a While ⏱️",
            """
            I will need to go through each of your save files for this chronicle to alter them with your current customizations (and restore the original thumbnail if you're removed a custom one). Depending on how many saves you still have in The Sims 4 for this chronicle, you may want to grab a magazine or pull up YouTube while I'm doing this.
            """).ConfigureAwait(false))
            return;
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRefresh, "Reapplying Customizations to Existing Saves", "json/archivist-constructing.json", "550px", "550px", Task.Run(async () => await Archivist.ReapplyEnhancementsAsync(chronicle).ConfigureAwait(false)));
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

    async Task ShowChronicleDatabaseAsync(Chronicle chronicle)
    {
        if (!await DialogService.ShowCautionDialogAsync("Please Be Cautious ✋",
            """
            Chronicle databases contain everything that is needed to bring your saves, no matter how long they've been gone from your game, back from the Netherworld 🪦. They are *memory*, 🐶 precious and 🌻 pure. I'll show you the one for this chronicle, but only if you *promise* me you'll treat it nicely.

            (Basically don't move it, delete it, open it... you know... give it odd looks. Be polite and keep your hands to yourself!)
            """))
            return;
        PlatformFunctions.ViewFile(new FileInfo(Path.Combine(Settings.ArchiveFolderPath, $"{MemoryMarshal.Read<ulong>(chronicle.FullInstance.AsSpan()):x16}.chronicle.sqlite")));
    }

    Task ShowSavePackageInSavesDirectoryAsync(Snapshot snapshot) => Task.Run(async () =>
    {
        var savesFolder = new DirectoryInfo(Path.Combine(Settings.UserDataFolderPath, "saves"));
        if (savesFolder.Exists)
        {
            if (!(await DialogService.ShowQuestionDialogAsync("This Might Take Some Time...",
                """
                I need to carefully examine the saves you currently have to see if I can find a match. I'll pop open your saves folder with it selected if I find it, or tell if you I couldn't find it.

                You sure you want me to go looking for it?
                """) ?? false))
                return;
            foreach (var file in savesFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var sha256 = await ModFileManifestModel.GetFileSha256HashAsync(file.FullName).ConfigureAwait(false);
                    if (sha256.SequenceEqual(snapshot.OriginalPackageSha256)
                        || sha256.SequenceEqual(snapshot.EnhancedPackageSha256))
                    {
                        PlatformFunctions.ViewFile(file);
                        return;
                    }
                }
                catch
                {
                }
            }
        }
        await DialogService.ShowInfoDialogAsync("This Save Exists Now Only in My Memory",
            """
            It seems the game decided to finally let this one pass on 🪽 to conserve your storage space. But, I can bring it back if you want 🫴, just restore or branch from this snapshot.
            """);
    });
}
