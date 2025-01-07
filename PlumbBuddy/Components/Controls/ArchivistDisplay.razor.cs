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
        if (Archivist.SelectedChronicle is not { } chronicle
            || await DialogService.ShowCreateBranchDialogAsync(chronicle.Name, chronicle.Notes ?? string.Empty, chronicle.GameNameOverride ?? string.Empty, chronicle.Thumbnail).ConfigureAwait(false) is not { } createBranchDialogResult)
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.CallSplit, "Creating and Slotting First Save Package", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.CreateBranchAsync(Settings, chronicle, createBranchDialogResult.ChronicleName, createBranchDialogResult.Notes, createBranchDialogResult.GameNameOverride, createBranchDialogResult.Thumbnail, taskCompletionSource.SetResult) is { } exportedFile)
            PlatformFunctions.ViewFile(exportedFile);
        else
        {
            taskCompletionSource.TrySetResult();
            await DialogService.ShowErrorDialogAsync("Whoops, Something Went Wrong", "If you want to know more, I may have written some technical mumbo-jumbo to my log file.");
        }
    }

    async Task DeletePreviousSnapshotsAsync(Snapshot snapshot)
    {
        if (!await DialogService.ShowCautionDialogAsync("Are you sure you want to do this?", "All snapshots in this chronicle prior to this one will be permanently lost."))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.TimelineRemove, "Deleting Previous Snapshots", "json/archivist-deleting.json", "550px", "550px", taskCompletionSource.Task);
        await Task.Delay(TimeSpan.FromSeconds(5));
        taskCompletionSource.SetResult();
        if (!await DialogService.ShowCautionDialogAsync("Okay I mean, are you REALLY sure?", "I **seriously** cannot undo this if you change your mind. This is your **last chance** to turn back."))
            return;
        taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.TimelineRemove, "Actually Deleting Previous Snapshots", "json/archivist-deleting.json", "550px", "550px", taskCompletionSource.Task);
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
        var taskCompletionSource = new TaskCompletionSource();
        try
        {
            DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRefresh, "Reapplying Customizations to Existing Saves", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
            await Task.Run(async () => await Archivist.ReapplyEnhancementsAsync(chronicle).ConfigureAwait(false));
            taskCompletionSource.SetResult();
        }
        catch (Exception ex)
        {
            taskCompletionSource.SetResult();
            Logger.LogError(ex, "encountered unexpected unhandled exception while reapplying enhancements for nucleus ID {NucleusId}, created {Created}", chronicle.NucleusId, chronicle.Created);
            await DialogService.ShowErrorDialogAsync("Whoops, Something Went Wrong", "If you want to know more, I may have written some technical mumbo-jumbo to my log file.");
        }
    }

    async Task RestoreSavePackageAsync(Snapshot snapshot)
    {
        if (Archivist.SelectedChronicle is not { } chronicle
            || !await DialogService.ShowCautionDialogAsync("Are you sure you don't want to create a branch?",
            """
            I will happily restore this snapshot for you, but you should know that *if I do*, The Sims 4 will regard it as the most recent save in this chronicle, even though *you and I both know* that it... really isn't. Things might get a little confusing in here as you continue to save after loading this restored snapshot, if you intend to keep "bouncing around".

            On the *other hand*, consider **creating a branch**. If your goal is to make a "What If?" style spin-off of your Sims' reality at a certain point in time and you intend to play both of them (even if one just occasionally), it will help you if you branch into a new chronicle. I'll change some numbers in the restored save file to make The Sims 4 see it as a different game, which will allow me to keep tracking of your save progression through both versions of reality separate and orderly 🧼. And, *on top of all that*, I'll actually end up using even *less* of your disk space with a separate chronicle for the other timeline, based on how my compression works.

            Or, ya know, maybe your intent here is to go back in time to correct a mistake and you have no interest in playing with the snapshots taken after this one. In that case, it's fine to just click the orange OK to accept that you've sat through my lecture 🥱 and to proceed with the restore.
            """))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileRestore, "Reconstructing and Slotting Save Package", "json/archivist-constructing.json", "550px", "550px", taskCompletionSource.Task);
        if (await snapshot.RestoreSavePackageAsync(Settings, chronicle, taskCompletionSource.SetResult) is { } restoredFile)
            PlatformFunctions.ViewFile(restoredFile);
        else
        {
            taskCompletionSource.TrySetResult();
            await DialogService.ShowErrorDialogAsync("Whoops, Something Went Wrong", "If you want to know more, I may have written some technical mumbo-jumbo to my log file.");
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
        PlatformFunctions.ViewFile(new FileInfo(Path.Combine(Settings.ArchiveFolderPath, $"N-{chronicle.NucleusId:x16}-C-{chronicle.Created:x16}.chronicle.sqlite")));
    }

    async Task ShowSavePackageInSavesDirectoryAsync(Snapshot snapshot)
    {
        var savesFolder = new DirectoryInfo(Path.Combine(Settings.UserDataFolderPath, "saves"));
        if (!savesFolder.Exists)
            return;
        if (!(await DialogService.ShowQuestionDialogAsync("This Might Take Some Time...",
            """
            I need to carefully examine the saves you currently have to see if I can find a match. I'll pop open your saves folder with it selected if I find it, or tell if you I couldn't find it.

            You sure you want me to go looking for it?
            """) ?? false))
            return;
        var taskCompletionSource = new TaskCompletionSource();
        DialogService.ShowBusyAnimationDialog("secondary-dialog", MudBlazor.Color.Secondary, MaterialDesignIcons.Normal.FileFind, "Having a Look at Your Saves Folder", "json/scan.json", "350px", "350px", taskCompletionSource.Task);
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
        await DialogService.ShowInfoDialogAsync("This Save Exists Now Only in My Memory",
            """
            It seems the game decided to finally let this one pass on 🪽 to conserve your storage space. But, I can bring it back if you want 🫴, just restore or branch from this snapshot.
            """);
    }
}
