namespace PlumbBuddy.Components.Controls.Parlay;

partial class ParlayPackage
{
    async Task MergeStringTableAsync()
    {
        if (await ModFileSelector.SelectAModFileAsync() is not { } modFile)
            return;
        if (!modFile.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
        {
            await DialogService.ShowErrorDialogAsync(AppText.Parlay_MergeStringTable_InvalidFileFormat_Caption, AppText.Parlay_MergeStringTable_InvalidFileFormat_Text);
            return;
        }
        var stringsImported = await Parlay.MergeStringTableAsync(modFile);
        if (stringsImported is 0)
        {
            await DialogService.ShowInfoDialogAsync(AppText.Parlay_MergeStringTable_NoStringsImported_Caption, AppText.Parlay_MergeStringTable_NoStringsImported_Text);
            return;
        }
        await DialogService.ShowInfoDialogAsync(AppText.Parlay_MergeStringTable_MergeComplete_Caption, string.Format(AppText.Parlay_MergeStringTable_MergeComplete_Text, AppText.Parlay_MergeStringTable_MergeComplete_Text_String.ToQuantity(stringsImported)));
    }

    async Task ShowCreatorMessageAsync()
    {
        var text = new List<string>();
        if (Parlay.MessageFromCreators is { } messageFromCreators)
            text.Add(messageFromCreators);
        if (Parlay.TranslationSubmissionUrl is { } transmissionSubmissionUrl)
            text.Add($"{AppText.Parlay_ShowCreatorMessage_Text_TranslationSubmissionUrlLabel}{Environment.NewLine}{transmissionSubmissionUrl}");
        await DialogService.ShowInfoDialogAsync(string.Format(AppText.Parlay_ShowCreatorMessage_Caption, Parlay.Creators ?? AppText.Parlay_ShowCreatorMessage_Caption_DefaultModCreators), string.Join($"{Environment.NewLine}{Environment.NewLine}", text));
    }

    async Task ShowOriginalPackageFileAsync()
    {
        if (Parlay.OriginalPackageFile is { } originalPackageFile)
        {
            originalPackageFile.Refresh();
            if (!originalPackageFile.Exists)
            {
                await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, AppText.Parlay_ShowOriginalPackage_NotFound_Text);
                return;
            }
            PlatformFunctions.ViewFile(originalPackageFile);
        }
    }

    async Task ShowTranslationPackageFileAsync()
    {
        if (Parlay.TranslationPackageFile is { } translationPackageFile)
        {
            translationPackageFile.Refresh();
            if (!translationPackageFile.Exists)
            {
                await DialogService.ShowErrorDialogAsync(AppText.Archivist_Error_Caption, AppText.Parlay_ShowTranslationPackage_NotFound_Text);
                return;
            }
            PlatformFunctions.ViewFile(translationPackageFile);
        }
    }
}
