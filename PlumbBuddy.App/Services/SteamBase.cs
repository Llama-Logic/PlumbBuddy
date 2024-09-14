using Gameloop.Vdf.Linq;
using Gameloop.Vdf;

namespace PlumbBuddy.App.Services;

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

    protected abstract DirectoryInfo? GetSteamDataDirectory();

    protected abstract FileSystemInfo GetTS4Executable(DirectoryInfo installationDirectory);

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
}
