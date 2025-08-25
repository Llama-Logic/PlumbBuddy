namespace PlumbBuddy.Services;

/// <summary>
/// Represents the a platform's implementation of interoperation with EA's App, which way have installed The Sims 4
/// </summary>
public interface IElectronicArtsApp
{

    /// <summary>
    /// Gets the EA App's user data directory if it exists
    /// </summary>
    /// <returns>The EA App's user data directory if it exists; otherwise, <see langword="null"/></returns>
    Task<DirectoryInfo?> GetElectronicArtsUserDataDirectoryAsync();

    /// <summary>
    /// Gets whether the EA App is installed on this computer
    /// </summary>
    /// <returns><see langword="true"/> if the EA App is installed on this computer; otherwise, <see langword="false"/></returns>
    Task<bool> GetIsElectronicArtsAppInstalledAsync();

    /// <summary>
    /// Gets the command line arguments configured in the EA App for The Sims 4
    /// </summary>
    /// <returns>The command line arguments for The Sims 4 in the EA App if they exist; otherwise, <see langword="null"/></returns>
    Task<string?> GetTS4ConfiguredCommandLineArgumentsAsync();

    /// <summary>
    /// Gets the Electronic Arts offer IDs the player's EA App has logged
    /// </summary>
    /// <returns>An immutable hash set containing the Electronic Arts offer IDs the player's EA app has logged if they are available; otherwise, <see langword="null"/></returns>
    Task<ImmutableHashSet<string>?> GetTS4ElectronicArtsOfferIdsAsync();

    /// <summary>
    /// Gets the installation directory for The Sims 4 if it was installed by the EA App on this computer
    /// </summary>
    /// <returns>A <see cref="DirectoryInfo"/> object encapsulating The Sims 4 installation directory as installed by the EA App if it was installed by the EA App; otherwise, <see langword="null"/></returns>
    Task<DirectoryInfo?> GetTS4InstallationDirectoryAsync();

    /// <summary>
    /// Launches the EA App's process if it doesn't already exist
    /// </summary>
    Task LaunchElectronicArtsAppAsync();

    /// <summary>
    /// Quits the EA App's process if it exists
    /// </summary>
    /// <returns><see langword="true"/> if the EA App was quit; otherwise, <see langword="false"/></returns>
    Task<bool> QuitElectronicArtsAppAsync();

    /// <summary>
    /// Sets the command line arguments configured in the EA App for The Sims 4
    /// </summary>
    Task SetTS4ConfiguredCommandLineArgumentsAsync(string? commandLineArguments);
}
