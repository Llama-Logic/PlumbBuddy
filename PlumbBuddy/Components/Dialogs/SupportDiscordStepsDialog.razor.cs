namespace PlumbBuddy.Components.Dialogs;

partial class SupportDiscordStepsDialog
{
    bool wasGeneratingGlobalManifestPackage;

    [Parameter]
    public string? CreatorName { get; set; }

    [Parameter]
    public FileInfo? ErrorFile { get; set; }

    bool GenerateGlobalManifestPackage
    {
        get => Settings.GenerateGlobalManifestPackage;
        set => Settings.GenerateGlobalManifestPackage = value;
    }

    [Parameter]
    public bool IsPatchDay { get; set; }

    [CascadingParameter]
    IMudDialogInstance? MudDialog { get; set; }

    Dictionary<string, Collection<SupportDiscordStep>> Steps =>
        CreatorName is { } creatorName
        && SupportDiscord!.SpecificCreators.TryGetValue(creatorName, out var specificCreator)
        && specificCreator.AskForHelpSteps.Count is > 0
        ? specificCreator.AskForHelpSteps
        : IsPatchDay
        && SupportDiscord!.PatchDayHelpSteps.Count is > 0
        ? SupportDiscord!.PatchDayHelpSteps
        : ErrorFile is not null
        && SupportDiscord!.TextFileSubmissionSteps.Count is > 0
        ? SupportDiscord!.TextFileSubmissionSteps
        : SupportDiscord!.AskForHelpSteps;

    [Parameter]
    public SupportDiscord? SupportDiscord { get; set; }

    [Parameter]
    public string SupportDiscordName { get; set; } = string.Empty;

    void CloseOnClickHandler() =>
        MudDialog?.Close();

    public void Dispose()
    {
        if (wasGeneratingGlobalManifestPackage)
            Settings.GenerateGlobalManifestPackage = true;
    }

    async Task HandleClearCacheOnClickAsync()
    {
        if (Settings.CacheStatus is SmartSimCacheStatus.Clear)
        {
            await DialogService.ShowInfoDialogAsync(AppText.SupportDiscordStepsDialog_ClearCache_AlreadyClear_Caption, AppText.SupportDiscordStepsDialog_ClearCache_AlreadyClear_Text);
            return;
        }
        if (await SmartSimObserver.ClearCacheAsync())
            await DialogService.ShowSuccessDialogAsync(AppText.SupportDiscordStepsDialog_ClearCache_Success_Caption, AppText.SupportDiscordStepsDialog_ClearCache_Success_Text);
    }

    Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (targetIndex == Steps.GetLanguageOptimalValue(() => new()).Count)
            MudDialog?.Close();
        return Task.FromResult(false);
    }

    async Task HandleShowAppLogFileOnClickAsync()
    {
        var appDataDirectory = MauiProgram.AppDataDirectory;
        if (!appDataDirectory.Exists)
        {
            await DialogService.ShowErrorDialogAsync(AppText.SupportDiscordStepsDialog_HighlightAppLogs_NoAppData_Caption, AppText.SupportDiscordStepsDialog_HighlightAppLogs_NoAppData_Text);
            return;
        }
        var mostRecentLogFile = appDataDirectory
            .GetFiles()
            .Where(f => (f.Name.StartsWith("Log", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("DebugLog", StringComparison.OrdinalIgnoreCase)) && f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();
        if (mostRecentLogFile is null || !mostRecentLogFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync(AppText.SupportDiscordStepsDialog_HighlightAppLogs_NoLogFile_Caption, AppText.SupportDiscordStepsDialog_HighlightAppLogs_NoLogFile_Text);
            return;
        }
        PlatformFunctions.ViewFile(mostRecentLogFile);
    }

    async Task HandleShowGameVersionFileOnClickAsync()
    {
        var gameVersionFile = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "GameVersion.txt"));
        if (!gameVersionFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync(AppText.SupportDiscordStepsDialog_HighlightGameVersion_NotFound_Caption, AppText.SupportDiscordStepsDialog_HighlightGameVersion_NotFound_Text);
            return;
        }
        PlatformFunctions.ViewFile(gameVersionFile);
    }

    async Task HandleShowErrorFileOnClickAsync()
    {
        if (ErrorFile is null)
            return;
        if (!ErrorFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync(AppText.SupportDiscordStepsDialog_HighlightErrorLog_NotFound_Caption, AppText.SupportDiscordStepsDialog_HighlightErrorLog_NotFound_Text);
            return;
        }
        PlatformFunctions.ViewFile(ErrorFile);
    }

    void HandleStartOverOnClick() =>
        MudDialog?.Close("start-over");

    protected override void OnInitialized() =>
        wasGeneratingGlobalManifestPackage = Settings.GenerateGlobalManifestPackage;
}
