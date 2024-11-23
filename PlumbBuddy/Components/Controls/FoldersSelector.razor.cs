namespace PlumbBuddy.Components.Controls;

partial class FoldersSelector
{
    MudForm? foldersForm;
    bool isEAAppInstalled;
    bool isFetchingInstallationFolder;
    bool isSteamInstalled;
    bool isTS4AvailableFromEA;
    bool isTS4AvailableFromValve;

    [Parameter]
    public string DownloadsFolderPath { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> DownloadsFolderPathChanged { get; set; }

    [Parameter]
    public string InstallationFolderPath { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> InstallationFolderPathChanged { get; set; }

    [Parameter]
    public bool IsOnboarding { get; set; }

    public bool IsValid { get; private set; }

    [Parameter]
    public string UserDataFolderPath { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> UserDataFolderPathChanged { get; set; }

    async Task BrowseForDownloadsFolderOnClickAsync()
    {
        var result = await FolderPicker.PickAsync(DownloadsFolderPath);
        if (result.Exception is not null)
            return;
        if (!result.IsSuccessful)
            return;
        DownloadsFolderPath = result.Folder.Path;
        await DownloadsFolderPathChanged.InvokeAsync(DownloadsFolderPath);
    }

    async Task BrowseForInstallationFolderOnClickAsync()
    {
        var result = await FolderPicker.PickAsync(InstallationFolderPath);
        if (result.Exception is not null)
            return;
        if (!result.IsSuccessful)
            return;
        InstallationFolderPath = result.Folder.Path;
        await InstallationFolderPathChanged.InvokeAsync(InstallationFolderPath);
    }

    async Task BrowseForUserDataFolderOnClickAsync()
    {
        var result = await FolderPicker.PickAsync(UserDataFolderPath);
        if (result.Exception is not null)
            return;
        if (!result.IsSuccessful)
            return;
        UserDataFolderPath = result.Folder.Path;
        await UserDataFolderPathChanged.InvokeAsync(UserDataFolderPath);
    }

    public async Task ScanForFoldersAsync()
    {
        var userDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", AppText.UserDataFolderName);
        if (File.Exists(Path.Combine(userDataFolderPath, "Options.ini")))
        {
            UserDataFolderPath = userDataFolderPath;
            await UserDataFolderPathChanged.InvokeAsync(UserDataFolderPath);
        }
        isEAAppInstalled = await ElectronicArtsApp.GetIsElectronicArtsAppInstalledAsync();
        var eaTS4InstallationDirectory = await ElectronicArtsApp.GetTS4InstallationDirectoryAsync();
        isTS4AvailableFromEA = eaTS4InstallationDirectory is not null;
        isSteamInstalled = await Steam.GetIsSteamInstalledAsync();
        var valveTS4InstallationDirectory = await Steam.GetTS4InstallationDirectoryAsync();
        isTS4AvailableFromValve = valveTS4InstallationDirectory is not null;
        if (eaTS4InstallationDirectory is not null)
        {
            InstallationFolderPath = eaTS4InstallationDirectory.FullName;
            await InstallationFolderPathChanged.InvokeAsync(InstallationFolderPath);
        }
        else if (valveTS4InstallationDirectory is not null)
        {
            InstallationFolderPath = valveTS4InstallationDirectory.FullName;
            await InstallationFolderPathChanged.InvokeAsync(InstallationFolderPath);
        }
        DownloadsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        await DownloadsFolderPathChanged.InvokeAsync(DownloadsFolderPath);
    }

    async Task UseDefaultUserDataAndDownloadsFoldersOnClickAsync()
    {
        UserDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", AppText.UserDataFolderName);
        await UserDataFolderPathChanged.InvokeAsync(UserDataFolderPath);
        DownloadsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        await DownloadsFolderPathChanged.InvokeAsync(DownloadsFolderPath);
        StateHasChanged();
    }

    async Task UseEAAppVersionOnClickAsync()
    {
        isFetchingInstallationFolder = true;
        StateHasChanged();
        if (await ElectronicArtsApp.GetTS4InstallationDirectoryAsync() is { } eaTS4InstallationDirectory)
        {
            InstallationFolderPath = eaTS4InstallationDirectory.FullName;
            await InstallationFolderPathChanged.InvokeAsync(InstallationFolderPath);
        }
        isFetchingInstallationFolder = false;
        StateHasChanged();
    }

    async Task UseSteamVersionOnClickAsync()
    {
        isFetchingInstallationFolder = true;
        StateHasChanged();
        if (await Steam.GetTS4InstallationDirectoryAsync() is { } valveTS4InstallationDirectory)
        {
            InstallationFolderPath = valveTS4InstallationDirectory.FullName;
            await InstallationFolderPathChanged.InvokeAsync(InstallationFolderPath);
        }
        isFetchingInstallationFolder = false;
        StateHasChanged();
    }

    public async Task ValidateAsync()
    {
        if (foldersForm is not null)
        {
            await foldersForm.Validate();
            IsValid = foldersForm.IsValid;
        }
    }

    string? ValidateDownloadsFolderPath(string path)
    {
        if (!Directory.Exists(path))
            return AppText.FoldersSelector_ValidateFolder_DoesNotExist;
        return null;
    }

    string? ValidateInstallationFolderPath(string path)
    {
        if (!Directory.Exists(path))
            return AppText.FoldersSelector_ValidateFolder_DoesNotExist;
#if WINDOWS
        if (!File.Exists(Path.Combine(path, "Game", "Bin", "TS4_x64.exe")))
            return AppText.FoldersSelector_ValidateInstallationFolder_NoBinary;
#elif MACCATALYST
        if (!File.Exists(Path.Combine(path, "Contents", "MacOS", "The Sims 4")))
            return AppText.FoldersSelector_ValidateInstallationFolder_NoBinary;
#else
        throw new NotSupportedException("The actual fu--");
#endif
        return null;
    }

    string? ValidateUserDataFolderPath(string path)
    {
        if (!Directory.Exists(path))
            return AppText.FoldersSelector_ValidateFolder_DoesNotExist;
        if (new DirectoryInfo(path) is { Name: "Mods", Exists: true } && File.Exists(Path.Combine(path, "..", "Options.ini")))
            return AppText.FoldersSelector_ValidateUserDataFolder_ModsFolder;
        if (!File.Exists(Path.Combine(path, "Options.ini")))
            return AppText.FoldersSelector_ValidateUserDataFolder_NoOptionsIni;
        return null;
    }
}
