namespace PlumbBuddy.Components.Dialogs;

static class DialogExtensions
{
    public static Task ShowAskForHelpDialogAsync(this IDialogService dialogService, Microsoft.Extensions.Logging.ILogger logger, IPublicCatalogs publicCatalogs, FileInfo? errorFile = null, bool isPatchDay = false, IReadOnlyList<string>? forCreators = null, string? forManifestHashHex = null) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            while (true)
            {
                if ((publicCatalogs.SupportDiscordsCacheTTL is not { } ttl || ttl < TimeSpan.FromMinutes(30)) && !(await ShowQuestionDialogAsync(dialogService, "Is it alright with you if I download the Community Discords list from plumbbuddy.app?", "The people that made me defer to the people who run the Community Discord servers, so I need to get the latest list of available Community Discord servers and what they expect of us. But, I want your permission to connect to the Internet to do that.") ?? false))
                    return;
                IReadOnlyDictionary<string, SupportDiscord> supportDiscords;
                try
                {
                    supportDiscords = await publicCatalogs.GetSupportDiscordsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "failed to retrieve Support Discords from PlumbBuddy.app");
                    await dialogService.ShowErrorDialogAsync("Whoops, Something Went Wrong!", "While I was trying to get the Support Discords list from my website, it just... didn't work. Umm... can we try this again later?");
                    return;
                }
                var (discordName, creatorName) = await ShowSelectSupportDiscordDialogAsync(dialogService, supportDiscords, errorFile, isPatchDay, forCreators, forManifestHashHex);
                if (discordName is null)
                    return;
                if (await ShowSupportDiscordStepsDialogAsync(dialogService, discordName, supportDiscords[discordName], errorFile, isPatchDay, creatorName) is "start-over")
                    continue;
                break;
            }
        });

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

    public static Task<IReadOnlyList<string>> ShowDeleteErrorLogsDialogAsync(this IDialogService dialogService, IReadOnlyList<string> filePaths, IEnumerable<string> preselectedFilePaths) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<DeleteErrorLogsDialog>("Delete Error Logs", new DialogParameters<DeleteErrorLogsDialog>()
            {
                { x => x.FilePaths, filePaths },
                { x => x.SelectedFilePaths, preselectedFilePaths }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Medium
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is IEnumerable<string> selectedFilePaths)
                return selectedFilePaths.ToImmutableArray();
            return (IReadOnlyList<string>)[];
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

    public static Task<bool?> ShowQuestionDialogAsync(this IDialogService dialogService, string caption, string text, bool userCanCancel = false, bool big = false) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<QuestionDialog>(caption, new DialogParameters<QuestionDialog>()
            {
                { x => x.Caption, caption },
                { x => x.Text, text },
                { x => x.UserCanCancel, userCanCancel }
            }, new DialogOptions
            {
                FullWidth = big,
                MaxWidth = big ? MaxWidth.Large : MaxWidth.Small
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

    public static Task<(string? discordName, string? creatorName)> ShowSelectSupportDiscordDialogAsync(this IDialogService dialogService, IReadOnlyDictionary<string, SupportDiscord> supportDiscords, FileInfo? errorFile = null, bool isPatchDay = false, IReadOnlyList<string>? forCreators = null, string? forManifestHashHex = null) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SelectSupportDiscordDialog>("Select a Support Venue", new DialogParameters<SelectSupportDiscordDialog>()
            {
                { x => x.ErrorFile, errorFile },
                { x => x.ForCreators, forCreators },
                { x => x.ForManifestHashHex, forManifestHashHex },
                { x => x.IsPatchDay, isPatchDay },
                { x => x.SupportDiscords, supportDiscords }
            }, new DialogOptions
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Large
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is ValueTuple<string, string> selectedDiscord)
                return ((string?)selectedDiscord.Item1, (string?)selectedDiscord.Item2);
            return (null, null);
        });

    public static Task<string?> ShowSupportDiscordStepsDialogAsync(this IDialogService dialogService, string supportDiscordName, SupportDiscord supportDiscord, FileInfo? errorFile = null, bool isPatchDay = false, string? creatorName = null) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SupportDiscordStepsDialog>(supportDiscordName, new DialogParameters<SupportDiscordStepsDialog>()
            {
                { x => x.CreatorName, creatorName },
                { x => x.ErrorFile, errorFile },
                { x => x.IsPatchDay, isPatchDay },
                { x => x.SupportDiscord, supportDiscord },
                { x => x.SupportDiscordName, supportDiscordName }
            }, new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Large,
                NoHeader = false
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is string outcome)
                return outcome;
            return null;
        });
}
