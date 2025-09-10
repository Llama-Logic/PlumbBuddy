namespace PlumbBuddy.Components.Dialogs;

static class DialogExtensions
{
    public static async Task<SupportDiscord?> GetSupportDiscordAsync(this IDialogService dialogService, Microsoft.Extensions.Logging.ILogger logger, IPublicCatalogs publicCatalogs, string name)
    {
        if (await GetSupportDiscordsAsync(dialogService, logger, publicCatalogs) is not { } supportDiscords
            || !supportDiscords.TryGetValue(name, out var supportDiscord))
            return null;
        return supportDiscord;
    }

    public static Task<IReadOnlyDictionary<string, SupportDiscord>?> GetSupportDiscordsAsync(this IDialogService dialogService, Microsoft.Extensions.Logging.ILogger logger, IPublicCatalogs publicCatalogs) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            if ((publicCatalogs.SupportDiscordsCacheTTL is not { } ttl || ttl < TimeSpan.FromMinutes(30)) && !(await ShowQuestionDialogAsync(dialogService, AppText.Dialogs_Question_DownloadSupportDiscords_Caption, AppText.Dialogs_Question_DownloadSupportDiscords_Text) ?? false))
                return null;
            try
            {
                return await publicCatalogs.GetSupportDiscordsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to retrieve Support Discords from PlumbBuddy.app");
                await dialogService.ShowErrorDialogAsync(AppText.Dialogs_Error_GetSupportDiscordsFailed_Caption, AppText.Dialogs_Error_GetSupportDiscordsFailed_Text);
                return null;
            }
        });

    public static Task ShowAskForHelpDialogAsync(this IDialogService dialogService, Microsoft.Extensions.Logging.ILogger logger, IPublicCatalogs publicCatalogs, FileInfo? errorFile = null, bool isPatchDay = false, IReadOnlyList<string>? forCreators = null, string? forManifestHashHex = null) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            while (true)
            {
                if (await GetSupportDiscordsAsync(dialogService, logger, publicCatalogs) is not { } supportDiscords)
                    return;
                var (discordName, creatorName) = await ShowSelectSupportDiscordDialogAsync(dialogService, supportDiscords, errorFile, isPatchDay, forCreators, forManifestHashHex);
                if (discordName is null)
                    return;
                if (await ShowSupportDiscordStepsDialogAsync(dialogService, discordName, supportDiscords[discordName], errorFile, isPatchDay, creatorName) is "start-over")
                    continue;
                break;
            }
        });

    public static void ShowBusyAnimationDialog(this IDialogService dialogService, string @class, MudBlazor.Color color, string iconSvg, string caption, string animationPath, string animationWidth, string animationHeight, Task processComplete) =>
        StaticDispatcher.Dispatch(async () =>
        {
            var dialog = await dialogService.ShowAsync<BusyAnimationDialog>(caption, new DialogParameters<BusyAnimationDialog>()
            {
                { x => x.Class, @class },
                { x => x.Color, color },
                { x => x.IconSvg, iconSvg },
                { x => x.Caption, caption },
                { x => x.AnimationPath, animationPath },
                { x => x.AnimationWidth, animationWidth },
                { x => x.AnimationHeight, animationHeight },
                { x => x.ProcessComplete, processComplete }
            }, new DialogOptions
            {
                BackdropClick = false,
                CloseOnEscapeKey = false,
                MaxWidth = MaxWidth.Small
            });
            await dialog.Result;
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

    public static Task<CreateBranchDialogResult?> ShowCreateBranchDialogAsync(this IDialogService dialogService, string chronicleName, string notes, string gameNameOverride, ImmutableArray<byte> thumbnail) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<CreateBranchDialog>("Create Branch", new DialogParameters<CreateBranchDialog>()
            {
                { x => x.ChronicleName, chronicleName },
                { x => x.Notes, notes },
                { x => x.GameNameOverride, gameNameOverride },
                { x => x.Thumbnail, thumbnail }
            }, new DialogOptions
            {
                MaxWidth = MaxWidth.Small
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is CreateBranchDialogResult createBranchDialogResult)
                return createBranchDialogResult;
            return null;
        });

    public static Task<IReadOnlyList<string>> ShowDeleteErrorLogsDialogAsync(this IDialogService dialogService, IReadOnlyList<string> filePaths, IEnumerable<string> preselectedFilePaths) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<DeleteErrorLogsDialog>(AppText.DeleteErrorLogsDialog_Caption, new DialogParameters<DeleteErrorLogsDialog>()
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

    public static Task<string?> ShowSelectCatalogedModFileDialogAsync(this IDialogService dialogService) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SelectCatalogedModFileDialog>(null, new DialogParameters<SelectCatalogedModFileDialog>()
            {
            }, new DialogOptions
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            });
            if (await dialog.Result is { } dialogResult
                && !dialogResult.Canceled
                && dialogResult.Data is string modFilePath)
                return modFilePath;
            return null;
        });

    public static Task<IReadOnlyList<string>?> ShowSelectFeaturesDialogAsync(this IDialogService dialogService, ModFileManifestModel manifest) =>
        StaticDispatcher.DispatchAsync(async () =>
        {
            var dialog = await dialogService.ShowAsync<SelectFeaturesDialog>(string.Format(AppText.SelectFeaturesDialog_Caption, manifest?.Name ?? AppText.SelectFeaturesDialog_Caption_FallbackModName), new DialogParameters<SelectFeaturesDialog>()
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
            var dialog = await dialogService.ShowAsync<SelectManifestDialog>(AppText.SelectManifestDialog_Caption, new DialogParameters<SelectManifestDialog>()
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

    public static async Task ShowSettingsDialogAsync(this IDialogService dialogService, int activePanelIndex = 0) =>
        await (await dialogService.ShowAsync<SettingsDialog>(string.Empty, new DialogParameters<SettingsDialog>()
        {
            { x => x.ActivePanelIndex, activePanelIndex }
        }, new DialogOptions
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
            var dialog = await dialogService.ShowAsync<SelectSupportDiscordDialog>(AppText.SelectSupportDiscordDialog_Caption, new DialogParameters<SelectSupportDiscordDialog>()
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
