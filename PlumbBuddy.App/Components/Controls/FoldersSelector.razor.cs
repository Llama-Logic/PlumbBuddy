namespace PlumbBuddy.App.Components.Controls;

partial class FoldersSelector
{
    MudForm? foldersForm;
    bool isEAAppInstalled;
    bool isFetchingInstallationFolder;
    bool isSteamInstalled;
    bool isTS4AvailableFromEA;
    bool isTS4AvailableFromValve;

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
    }

    async Task UseDefaultUserDataFolderOnClickAsync()
    {
        UserDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Electronic Arts", AppText.UserDataFolderName);
        await UserDataFolderPathChanged.InvokeAsync(UserDataFolderPath);
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

    string? ValidateInstallationFolderPath(string path)
    {
        if (!Directory.Exists(path))
            return "Bruh... 🤦... there's not even a folder there.";
        if (File.Exists(Path.Combine(path, "Options.ini")))
            return "Hmm, I think maybe we've gotten our 🦁s crossed. THAT, my friend, is your User Data folder, not your Installation Folder.";
        if (new DirectoryInfo(path) is { Name: "Mods", Exists: true } directory && File.Exists(Path.Combine(path, "..", "Options.ini")))
            return "😖 Oy, that's your Mods folder. I need the path to where your game is installed in this field, pal.";
#if WINDOWS
        if (!File.Exists(Path.Combine(path, "Game", "Bin", "TS4_x64.exe")))
            return "That's not a valid The Sims 4 installation. 🙄";
#elif MACCATALYST
        // TODO: Grovel to someone smarter than me to implement this for macOS
#else
        throw new NotSupportedException("The actual fu--");
#endif
        return null;
    }

    string? ValidateUserDataFolderPath(string path)
    {
        if (!Directory.Exists(path))
            return "Bruh... 🤦... there's not even a folder there.";
#if WINDOWS
        if (File.Exists(Path.Combine(path, "Game", "Bin", "TS4_x64.exe")))
            return "Woah, woah, woah. 🤚 That's your Installation Folder, not your User Data Folder.";
#elif MACCATALYST
        // TODO: Grovel to someone smarter than me to implement this for macOS
#else
        throw new NotSupportedException("The actual fu--");
#endif
        if (new DirectoryInfo(path) is { Name: "Mods", Exists: true } directory && File.Exists(Path.Combine(path, "..", "Options.ini")))
            return "👏 Very ambitious for taking me right to your Mods folder, but I actually need your User Data Folder (go up one, please!).";
        if (!File.Exists(Path.Combine(path, "Options.ini")))
            return "Hey, Silly! That's not your Sims 4 User Data Folder. 😏 Or you need to launch the game once and try again...";
        return null;
    }
}
