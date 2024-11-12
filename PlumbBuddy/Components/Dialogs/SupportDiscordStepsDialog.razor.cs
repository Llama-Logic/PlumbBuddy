namespace PlumbBuddy.Components.Dialogs;

partial class SupportDiscordStepsDialog
{
    [Parameter]
    public string? CreatorName { get; set; }

    [Parameter]
    public FileInfo? ErrorFile { get; set; }

    [Parameter]
    public bool IsPatchDay { get; set; }

    [CascadingParameter]
    MudDialogInstance? MudDialog { get; set; }

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
    public string? SupportDiscordName { get; set; }

    void CloseOnClickHandler() =>
        MudDialog?.Close();

    async Task HandleClearCacheOnClickAsync()
    {
        if (Settings.CacheStatus is SmartSimCacheStatus.Clear)
        {
            await DialogService.ShowInfoDialogAsync("The Cache is Already Clear", "Good on you for being thorough, though!");
            return;
        }
        SmartSimObserver.ClearCache();
        await DialogService.ShowSuccessDialogAsync("The Cache is Now Clear", "Well done, you now have a clean slate.");
    }

    Task<bool> HandlePreventStepChangeAsync(StepChangeDirection direction, int targetIndex)
    {
        if (targetIndex == Steps.GetLanguageOptimalValue(() => new()).Count)
            MudDialog?.Close();
        return Task.FromResult(false);
    }

    async Task HandleShowAppLogFileOnClickAsync()
    {
        var appDataDirectory = new DirectoryInfo(FileSystem.AppDataDirectory);
        if (!appDataDirectory.Exists)
        {
            await DialogService.ShowErrorDialogAsync("I Couldn't Find My Own App Data Directory", "Holy cow, how are you even getting to this error message?");
            return;
        }
        var mostRecentLogFile = appDataDirectory
            .GetFiles()
            .Where(f => (f.Name.StartsWith("Log", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("DebugLog", StringComparison.OrdinalIgnoreCase)) && f.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();
        if (mostRecentLogFile is null || !mostRecentLogFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync("I Couldn't Find My Most Recent Log File", "That uhh... shouldn't be a thing. So sorry.");
            return;
        }
        PlatformFunctions.ViewFile(mostRecentLogFile);
    }

    async Task HandleShowGameVersionFileOnClickAsync()
    {
        var gameVersionFile = new FileInfo(Path.Combine(Settings.UserDataFolderPath, "GameVersion.txt"));
        if (!gameVersionFile.Exists)
        {
            await DialogService.ShowErrorDialogAsync("I Couldn't Find Your Game Version File", "It looks like you need to launch The Sims 4 so that it will write that file. Once you've done that, come back here and click this button again.");
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
            await DialogService.ShowErrorDialogAsync("I Couldn't Find Your Error File", "Something must have happened to it, I'm so sorry.");
            return;
        }
        PlatformFunctions.ViewFile(ErrorFile);
    }

    void HandleStartOverOnClick() =>
        MudDialog?.Close("start-over");
}
