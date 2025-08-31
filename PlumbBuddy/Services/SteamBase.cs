using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using LlamaLogic.ValveDataFormat;

namespace PlumbBuddy.Services;

abstract class SteamBase :
    ISteam
{
    const string steamAppId = "1222670";

    public Task<bool> GetIsSteamInstalledAsync()
    {
        if (GetSteamDataDirectory() is { } steamDataDirectory && steamDataDirectory.Exists)
            return Task.FromResult(true);
        return Task.FromResult(false);
    }

    public abstract Task<bool> GetIsSteamRunningAsync();

    protected abstract DirectoryInfo? GetSteamDataDirectory();

    protected abstract FileSystemInfo GetTS4Executable(DirectoryInfo installationDirectory);

    public async Task<string?> GetTS4ConfiguredCommandLineArgumentsAsync()
    {
        var steamDirectory = GetSteamDataDirectory();
        if (steamDirectory is null)
            return null;
        var userDataDirectory = new DirectoryInfo(Path.Combine(steamDirectory.FullName, "userdata"));
        if (!userDataDirectory.Exists)
            return null;
        foreach (var userDirectory in userDataDirectory.GetDirectories())
        {
            var configDirectory = new DirectoryInfo(Path.Combine(userDirectory.FullName, "config"));
            if (!configDirectory.Exists)
                continue;
            var localConfigFile = new FileInfo(Path.Combine(configDirectory.FullName, "localconfig.vdf"));
            if (!localConfigFile.Exists)
                continue;
            IReadOnlyList<VdfNode> localConfig;
            using (var localConfigFileStream = localConfigFile.OpenRead())
            using (var localConfigFileReader = new StreamReader(localConfigFileStream))
                localConfig = await VdfNode.DeserializeAsync(localConfigFileReader).ConfigureAwait(false);
            if (localConfig.Count is 0
                || localConfig[0] is not VdfKeyValuePair userLocalConfigStore
                || userLocalConfigStore.Key is not "UserLocalConfigStore"
                || userLocalConfigStore.Value is not VdfSection userLocalConfigStoreSection)
                continue;
            try
            {
                if (userLocalConfigStoreSection["Software"] is not VdfSection softwareSection
                    || softwareSection["Valve"] is not VdfSection valveSection
                    || valveSection["Steam"] is not VdfSection steamSection
                    || steamSection["apps"] is not VdfSection appsSection
                    || appsSection[steamAppId] is not VdfSection ts4Section)
                    continue;
                try
                {
                    if (ts4Section["LaunchOptions"] is string launchOptionsValue)
                        return launchOptionsValue;
                }
                catch (InvalidOperationException)
                {
                    // don't care
                }
                return null;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
        }
        return null;
    }

    public async Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync()
    {
        var steamDirectory = GetSteamDataDirectory();
        if (steamDirectory is null)
            return null; // no Steam directory? pffft...
        var libraryFoldersVdfFile = new FileInfo(Path.Combine(steamDirectory.FullName, "config", "libraryfolders.vdf"));
        if (!libraryFoldersVdfFile.Exists)
            return null; // well, well, well... never seen this missing before... bye, Felecia!
        var libraryFoldersVdfFileText = await File.ReadAllTextAsync(libraryFoldersVdfFile.FullName).ConfigureAwait(false);
        if (VdfConvert.Deserialize(libraryFoldersVdfFileText) is not { } libraryFolders)
            return null; // corrupted Steam installation? YIKES...
        foreach (var libraryFolder in libraryFolders.Value.Cast<VProperty>())
        {
            if (libraryFolder.Value is not VObject libraryFolderObject)
                continue; // ha ha ha... what?
            if (libraryFolderObject["apps"] is not VObject appsObject)
                continue; // a library with no apps? marvelous, you do you... moving on...
            if (appsObject[steamAppId] is null)
                continue; // okay, TS4 just is not installed in this Steam library folder, fine, moving on...
            if (libraryFolderObject["path"] is not VValue pathValue)
                continue; // how do you purport to have a library folder without a path?
            if (pathValue.Value is not string path)
                continue; // how do you purport to have a path property without a value?
            var ts4SteamManifestPath = Path.Combine(path, "steamapps", $"appmanifest_{steamAppId}.acf");
            if (!File.Exists(ts4SteamManifestPath))
                continue; // so it's enumerated in the library, but the manifest is missing? okay, moving on...
            var ts4SteamManifestText = await File.ReadAllTextAsync(ts4SteamManifestPath).ConfigureAwait(false);
            if (VdfConvert.Deserialize(ts4SteamManifestText) is not { } ts4SteamManifest)
                continue; // corrupted manifest? okay, moving on...
            if (ts4SteamManifest.Value is not VObject ts4SteamManifestObject)
                continue; // corrupted manifest? okay, moving on...
            if (ts4SteamManifestObject["installdir"] is not VValue installdirValue)
                continue; // so you're installed... nowhere? okay, moving on...
            if (installdirValue.Value is not string installdir)
                continue; // or where you're installed is the string equivalent of an imaginary number? cool... moving on...
            var ts4Directory = new DirectoryInfo(Path.Combine(path, "steamapps", "common", installdir));
            if (!ts4Directory.Exists)
                continue; // bro, do you even TS4 or not really? frick'n poser...
            if (!GetTS4Executable(ts4Directory).Exists)
                continue; // oh, Jesus, EA, could you PLEASE, for once, CLEAN UP AFTER YOURSELF?!
            return ts4Directory; // well, if we got here, this shit is Classic Coke -- the real TS4, baby
        }
        return null; // sigh, ran all out of Steam libraries and no TS4 to be found 😔
    }

    public abstract Task LaunchSteamAsync();

    public abstract Task<bool> QuitSteamAsync();

    public async Task SetTS4ConfiguredCommandLineArgumentsAsync(string? commandLineArguments)
    {
        var quitSteam = await QuitSteamAsync().ConfigureAwait(false);
        var delayTicks = 20;
        while (await GetIsSteamRunningAsync().ConfigureAwait(false) && --delayTicks > 0)
            await Task.Delay(TimeSpan.FromSeconds(0.25)).ConfigureAwait(false);
        var steamDirectory = GetSteamDataDirectory();
        if (steamDirectory is null)
            return;
        var userDataDirectory = new DirectoryInfo(Path.Combine(steamDirectory.FullName, "userdata"));
        if (!userDataDirectory.Exists)
            return;
        foreach (var userDirectory in userDataDirectory.GetDirectories())
        {
            var configDirectory = new DirectoryInfo(Path.Combine(userDirectory.FullName, "config"));
            if (!configDirectory.Exists)
                continue;
            var localConfigFile = new FileInfo(Path.Combine(configDirectory.FullName, "localconfig.vdf"));
            if (!localConfigFile.Exists)
                continue;
            IReadOnlyList<VdfNode> localConfig;
            using (var localConfigFileStream = localConfigFile.OpenRead())
            using (var localConfigFileReader = new StreamReader(localConfigFileStream))
                localConfig = await VdfNode.DeserializeAsync(localConfigFileReader).ConfigureAwait(false);
            if (localConfig.Count is 0
                || localConfig[0] is not VdfKeyValuePair userLocalConfigStore
                || userLocalConfigStore.Key is not "UserLocalConfigStore"
                || userLocalConfigStore.Value is not VdfSection userLocalConfigStoreSection)
                continue;
            var localConfigChanged = false;
            try
            {
                if (userLocalConfigStoreSection["Software"] is not VdfSection softwareSection
                    || softwareSection["Valve"] is not VdfSection valveSection
                    || valveSection["Steam"] is not VdfSection steamSection
                    || steamSection["apps"] is not VdfSection appsSection
                    || appsSection[steamAppId] is not VdfSection ts4Section)
                    continue;
                if (ts4Section.Nodes.FirstOrDefault(node => node is VdfKeyValuePair kvp && kvp.Key == "LaunchOptions") is VdfKeyValuePair launchOptionsKeyValuePair)
                {
                    if (string.IsNullOrWhiteSpace(commandLineArguments))
                    {
                        ts4Section.Nodes.Remove(launchOptionsKeyValuePair);
                        localConfigChanged = true;
                    }
                    else if (launchOptionsKeyValuePair.Value is not string value
                        || value != commandLineArguments)
                    {
                        launchOptionsKeyValuePair.Value = commandLineArguments;
                        localConfigChanged = true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(commandLineArguments))
                {
                    ts4Section.Nodes.Add(new VdfKeyValuePair
                    {
                        Key = "LaunchOptions",
                        Value = commandLineArguments
                    });
                    localConfigChanged = true;
                }
            }
            catch (InvalidOperationException)
            {
                // don't care
            }
            if (localConfigChanged)
            {
                using var localConfigFileStream = new FileStream(localConfigFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
                using var localConfigFileWriter = new StreamWriter(localConfigFileStream);
                await localConfig[0].SerializeAsync(localConfigFileWriter).ConfigureAwait(false);
                await localConfigFileWriter.FlushAsync().ConfigureAwait(false);
            }
        }
        if (quitSteam)
            await LaunchSteamAsync().ConfigureAwait(false);
    }
}
