namespace PlumbBuddy.Components.Controls.Archivist;

partial class ArchivistChronicleHeader
{
    bool isEditingChronicle;

    [Parameter]
    public Chronicle? Chronicle { get; set; }

    void NavigateToBasis(string? emphasis)
    {
        if (Archivist.SelectedChronicle is { } selectedChronicle
            && selectedChronicle.BasedOnSnapshot is { } basedOnSnapshot)
        {
            Archivist.SelectedChronicle = basedOnSnapshot.Chronicle;
            if (emphasis == "snapshot")
            {
                Archivist.SnapshotsSearchText = basedOnSnapshot.LastWriteTime.ToString("g");
                basedOnSnapshot.ShowDetails = true;
            }
            else
                Archivist.SnapshotsSearchText = string.Empty;
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

    async Task ShowChronicleDatabaseAsync(Chronicle chronicle)
    {
        if (!await DialogService.ShowCautionDialogAsync(AppText.Archivist_ShowChronicleDatabase_Caution_Caption, AppText.Archivist_ShowChronicleDatabase_Caution_Text))
            return;
        PlatformFunctions.ViewFile(new FileInfo(Path.Combine(Settings.ArchiveFolderPath, $"N-{chronicle.NucleusId:x16}-C-{chronicle.Created:x16}.chronicle.sqlite")));
    }
}
