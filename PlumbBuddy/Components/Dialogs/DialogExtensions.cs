namespace PlumbBuddy.Components.Dialogs;

static class DialogExtensions
{
    public static Task<bool> ShowCautionDialogAsync(this IDialogService dialogService, string caption, string text) =>
        StaticDispatcher.DispatchAsync(async () =>
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
        });

    public static Task ShowErrorDialogAsync(this IDialogService dialogService, string caption, string text) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<ErrorDialog>(caption, new DialogParameters<ErrorDialog>()
            {
                { x => x.Caption, caption },
                { x => x.Text, text }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Small
            });
            await dialog.Result;
        });

    public static Task ShowInfoDialogAsync(this IDialogService dialogService, string caption, string text) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<InfoDialog>(caption, new DialogParameters<InfoDialog>()
            {
                { x => x.Caption, caption },
                { x => x.Text, text }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Small
            });
            await dialog.Result;
        });

    public static async Task ShowOnboardingDialogAsync(this IDialogService dialogService) =>
        await (await dialogService.ShowAsync<OnboardingDialog>(string.Empty, new DialogOptions
        {
            CloseOnEscapeKey = false,
            BackdropClick = false,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
            NoHeader = false
        })).Result;

    public static Task<bool?> ShowQuestionDialogAsync(this IDialogService dialogService, string caption, string text) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<QuestionDialog>(caption, new DialogParameters<QuestionDialog>()
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
                return (bool?)bData;
            return null;
        });

    public static Task<IReadOnlyList<string>?> ShowSelectFeaturesDialogAsync(this IDialogService dialogService, ModFileManifestModel manifest) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SelectFeaturesDialog>($"Select {manifest?.Name ?? "Mod"} Features", new DialogParameters<SelectFeaturesDialog>()
            {
                { x => x.Manifest, manifest }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Medium
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is IReadOnlyList<string> features)
                return features;
            return default;
        });

    public static Task<ResourceKey> ShowSelectManifestDialogAsync(this IDialogService dialogService, IReadOnlyDictionary<ResourceKey, ModFileManifestModel> manifests) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SelectManifestDialog>("Select Manifest", new DialogParameters<SelectManifestDialog>()
            {
                { x => x.Manifests, manifests }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Medium
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is ResourceKey resourceKey)
                return resourceKey;
            return default;
        });

    public static async Task ShowSettingsDialogAsync(this IDialogService dialogService) =>
        await (await dialogService.ShowAsync<SettingsDialog>(string.Empty, new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.Medium,
            NoHeader = false,
            Position = DialogPosition.TopCenter
        })).Result;

    public static Task ShowSuccessDialogAsync(this IDialogService dialogService, string caption, string text) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SuccessDialog>(caption, new DialogParameters<SuccessDialog>()
            {
                { x => x.Caption, caption },
                { x => x.Text, text }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Small
            });
            await dialog.Result;
        });
}
