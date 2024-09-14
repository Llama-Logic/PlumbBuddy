namespace PlumbBuddy.App.Components.Dialogs;

static class DialogExtensions
{
    public static async Task<bool> ShowCautionDialogAsync(this IDialogService dialogService, string caption, string text)
    {
        var dialog = await dialogService.ShowAsync<CautionDialog>(caption, new DialogParameters<CautionDialog>()
        {
            { x => x.Caption, caption },
            { x => x.Text, text }
        }, new DialogOptions
        {
            MaxWidth = MaxWidth.Small
        });
        if (await dialog.Result is { } dialogResult
            && !dialogResult.Canceled
            && dialogResult.Data is bool bData)
            return bData;
        return false;
    }

    public static async Task ShowOnboardingDialogAsync(this IDialogService dialogService) =>
        await (await dialogService.ShowAsync<OnboardingDialog>(string.Empty, new DialogOptions
        {
            CloseOnEscapeKey = false,
            BackdropClick = false,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
            NoHeader = false
        })).Result;

    public static async Task ShowSettingsDialogAsync(this IDialogService dialogService) =>
        await (await dialogService.ShowAsync<SettingsDialog>(string.Empty, new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
            NoHeader = false,
            Position = DialogPosition.TopCenter
        })).Result;
}
