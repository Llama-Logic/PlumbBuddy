namespace PlumbBuddy.Services;

/// <summary>
/// Represents the a platform's implementation of interoperation with Valve's Steam client, which way have The Sims 4 installed among its libraries
/// </summary>
public interface ISteam
{
    /// <summary>
    /// Gets whether Steam is installed on this computer
    /// </summary>
    /// <returns><see langword="true"/> if Steam is installed on this computer; otherwise, <see langword="false"/></returns>
    Task<bool> GetIsSteamInstalledAsync();

    /// <summary>
    /// Gets whether Steam is running on this computer
    /// </summary>
    /// <returns><see langword="true"/> if Steam is running on this computer; otherwise, <see langword="false"/></returns>
    Task<bool> GetIsSteamRunningAsync();

    /// <summary>
    /// Gets the command line arguments configured in Steam for The Sims 4
    /// </summary>
    /// <returns>The command line arguments for The Sims 4 in Steam if they exist; otherwise, <see langword="null"/></returns>
    Task<string?> GetTS4ConfiguredCommandLineArgumentsAsync();

    /// <summary>
    /// Gets the installation directory for The Sims 4 if it was installed by Steam on this computer
    /// </summary>
    /// <returns>A <see cref="DirectoryInfo"/> object encapsulating The Sims 4 installation directory as installed by Steam if it was installed by Steam; otherwise, <see langword="null"/></returns>
    Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync();

    /// <summary>
    /// Launches Steams's process if it doesn't already exist
    /// </summary>
    Task LaunchSteamAsync();

    /// <summary>
    /// Quits Steam's process if it exists
    /// </summary>
    /// <returns><see langword="true"/> if the EA App was quit; otherwise, <see langword="false"/></returns>
    Task<bool> QuitSteamAsync();

    /// <summary>
    /// Sets the command line arguments configured in Steam for The Sims 4
    /// </summary>
    Task SetTS4ConfiguredCommandLineArgumentsAsync(string? commandLineArguments);
}
