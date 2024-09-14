namespace PlumbBuddy.App.Services;

/// <summary>
/// Represents the a platform's implementation of interoperation with Valve's Steam client, which way have The Sims 4 installed among its libraries
/// </summary>
interface ISteam
{
    /// <summary>
    /// Gets whether Steam is installed on this computer
    /// </summary>
    /// <returns><see langword="true"/> if Steam is installed on this computer; otherwise, <see langword="false"/></returns>
    Task<bool> GetIsSteamInstalledAsync();

    /// <summary>
    /// Gets the installation directory for The Sims 4 if it was installed by Steam on this computer
    /// </summary>
    /// <returns>A <see cref="DirectoryInfo"/> object encapsulating The Sims 4 installation directory as installed by Steam if it was installed by Steam; otherwise, <see langword="null"/></returns>
    Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync();
}
